import { api } from './api.js';
import {
    renderHeader, setCartBadge, formatPrice, escapeHtml, productImage, bgFor,
    plural, toast, toastError,
} from './common.js';

const CHECKOUT_DRAFT_KEY = 'shop.checkoutDraft'; // запоминаем контактные данные покупателя
const $root = document.getElementById('cart-root');

renderHeader('cart');
load();

async function load() {
    try {
        render(await api.cart.get());
    } catch (e) {
        $root.innerHTML = '';
        toastError(e);
    }
}

function render(cart) {
    setCartBadge(cart);

    if (cart.items.length === 0) {
        $root.innerHTML = `
            <div class="card empty">
                <div class="icon">🛒</div>
                <p>Корзина пуста</p>
                <a href="/" class="btn">Перейти в каталог</a>
            </div>`;
        return;
    }

    const hasProblems = cart.items.some(i => !i.isAvailable);

    $root.innerHTML = `
        <div class="cart-layout">
            <div>
                <div class="card cart-items">
                    ${cart.items.map(renderItem).join('')}
                </div>
                <div style="margin-top:12px">
                    <button class="btn btn-outline btn-sm" id="clear-cart">Очистить корзину</button>
                </div>
            </div>
            <aside class="card summary">
                <div class="summary-row">
                    <span>${cart.totalQuantity} ${plural(cart.totalQuantity, ['товар', 'товара', 'товаров'])}</span>
                    <span>${formatPrice(cart.total)}</span>
                </div>
                <div class="summary-row"><span>Доставка</span><span>бесплатно</span></div>
                <div class="summary-total"><span>Итого</span><span>${formatPrice(cart.total)}</span></div>
                <div id="checkout"></div>
            </aside>
        </div>`;

    mountCheckout(hasProblems);
}

function renderItem(item) {
    let warning = '';
    if (!item.isAvailable) {
        warning = item.stock === 0
            ? '<div class="warn">Нет в наличии — удалите из корзины</div>'
            : `<div class="warn">В наличии только ${item.stock} шт.</div>`;
    }

    return `
        <div class="cart-item" data-id="${item.productId}">
            <div class="cart-thumb" style="background:${bgFor(item.productId)}">${productImage(item)}</div>
            <div>
                <div style="font-weight:600">${escapeHtml(item.name)}</div>
                <div style="color:var(--muted);font-size:14px">${formatPrice(item.price)} / шт.</div>
                ${warning}
            </div>
            <div class="qty">
                <button data-delta="-1" aria-label="Меньше">−</button>
                <input type="number" value="${item.quantity}" min="1" max="${item.stock}" data-qty-input>
                <button data-delta="1" aria-label="Больше" ${item.quantity >= item.stock ? 'disabled' : ''}>+</button>
            </div>
            <div class="line-total">${formatPrice(item.lineTotal)}</div>
            <button class="remove-btn" data-remove title="Удалить">✕</button>
        </div>`;
}

// ---------- Изменение количества / удаление ----------

$root.addEventListener('click', async e => {
    if (e.target.id === 'clear-cart') {
        if (!confirm('Удалить все товары из корзины?')) return;
        await run(async () => { await api.cart.clear(); return api.cart.get(); });
        return;
    }

    const row = e.target.closest('.cart-item');
    if (!row) return;
    const productId = Number(row.dataset.id);

    if (e.target.closest('[data-remove]')) {
        await run(() => api.cart.remove(productId));
    } else if (e.target.closest('[data-delta]')) {
        const input = row.querySelector('[data-qty-input]');
        const next = Number(input.value) + Number(e.target.closest('[data-delta]').dataset.delta);
        await run(() => api.cart.setQuantity(productId, Math.max(0, next)));
    }
});

$root.addEventListener('change', async e => {
    if (!e.target.matches('[data-qty-input]')) return;
    const productId = Number(e.target.closest('.cart-item').dataset.id);
    const qty = Math.max(0, Math.floor(Number(e.target.value) || 0));
    await run(() => api.cart.setQuantity(productId, qty));
});

/** Выполняет действие с корзиной и перерисовывает её. При ошибке — перезагружает актуальное состояние. */
async function run(action) {
    try {
        render(await action());
    } catch (e) {
        toastError(e);
        load();
    }
}

// ---------- Оформление заказа ----------

function mountCheckout(hasProblems) {
    const container = document.getElementById('checkout');
    container.append(document.getElementById('checkout-template').content.cloneNode(true));

    const form = document.getElementById('checkout-form');
    const btn = document.getElementById('checkout-btn');
    const errorBox = document.getElementById('form-error');

    // Подставляем сохранённые данные покупателя
    const draft = JSON.parse(localStorage.getItem(CHECKOUT_DRAFT_KEY) || '{}');
    for (const [name, value] of Object.entries(draft)) {
        if (form.elements[name]) form.elements[name].value = value;
    }

    if (hasProblems) {
        btn.disabled = true;
        errorBox.textContent = 'Исправьте позиции, отмеченные красным, чтобы оформить заказ.';
    }

    form.addEventListener('submit', async e => {
        e.preventDefault();
        errorBox.textContent = '';

        // Клиентская валидация (сервер всё равно проверит повторно)
        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }

        const data = Object.fromEntries(new FormData(form));
        const { comment, ...contacts } = data;
        localStorage.setItem(CHECKOUT_DRAFT_KEY, JSON.stringify(contacts));

        btn.disabled = true;
        btn.textContent = 'Оформляем…';
        try {
            const order = await api.orders.create(data);
            toast(`Заказ №${order.id} оформлен!`, 'success');
            location.href = `/orders.html?created=${order.id}`;
        } catch (err) {
            btn.disabled = false;
            btn.textContent = 'Оформить заказ';
            if (err.message.startsWith('Не удалось оформить')) {
                // Остатки изменились, пока покупатель был на странице — перерисуем корзину с пометками
                toastError(err);
                load();
            } else {
                errorBox.textContent = err.message; // ошибки валидации полей
            }
        }
    });
}
