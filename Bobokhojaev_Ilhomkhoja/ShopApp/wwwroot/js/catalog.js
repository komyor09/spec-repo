import { api } from './api.js';
import {
    renderHeader, refreshCartBadge, setCartBadge, formatPrice, escapeHtml,
    productImage, bgFor, toast, toastError,
} from './common.js';

const PAGE_SIZE = 8;

// Состояние фильтров
const state = { categoryId: null, search: '', sort: 'new', page: 1 };

const $products = document.getElementById('products');
const $pagination = document.getElementById('pagination');
const $categories = document.getElementById('categories');
const $modal = document.getElementById('modal');
const $modalBody = document.getElementById('modal-body');

renderHeader('catalog');
refreshCartBadge();
init();

async function init() {
    try {
        const categories = await api.categories();
        renderCategories(categories);
    } catch (e) {
        toastError(e);
    }
    loadProducts();
}

// ---------- Категории ----------

function renderCategories(categories) {
    const all = [{ id: null, name: 'Все', icon: '✨' }, ...categories];
    $categories.innerHTML = all.map(c => `
        <button class="chip ${c.id === state.categoryId ? 'active' : ''}" data-id="${c.id ?? ''}">
            ${escapeHtml(c.icon)} ${escapeHtml(c.name)}
        </button>`).join('');
}

$categories.addEventListener('click', e => {
    const chip = e.target.closest('.chip');
    if (!chip) return;
    state.categoryId = chip.dataset.id ? Number(chip.dataset.id) : null;
    state.page = 1;
    $categories.querySelectorAll('.chip').forEach(c => c.classList.toggle('active', c === chip));
    loadProducts();
});

// ---------- Поиск и сортировка ----------

let searchTimer;
document.getElementById('search').addEventListener('input', e => {
    clearTimeout(searchTimer);
    searchTimer = setTimeout(() => {          // debounce — не дёргаем API на каждую букву
        state.search = e.target.value.trim();
        state.page = 1;
        loadProducts();
    }, 300);
});

document.getElementById('sort').addEventListener('change', e => {
    state.sort = e.target.value;
    state.page = 1;
    loadProducts();
});

// ---------- Товары ----------

async function loadProducts() {
    $products.innerHTML = '<div class="loader">Загрузка…</div>';
    try {
        const result = await api.products({
            categoryId: state.categoryId,
            search: state.search,
            sort: state.sort,
            page: state.page,
            pageSize: PAGE_SIZE,
        });
        renderProducts(result.items);
        renderPagination(result.total);
    } catch (e) {
        $products.innerHTML = '';
        toastError(e);
    }
}

function stockLabel(stock) {
    return stock > 0
        ? `<span class="stock in">В наличии: ${stock} шт.</span>`
        : `<span class="stock out">Нет в наличии</span>`;
}

function renderProducts(items) {
    if (items.length === 0) {
        $products.innerHTML = `
            <div class="empty" style="grid-column: 1 / -1">
                <div class="icon">🔍</div>Ничего не найдено
            </div>`;
        return;
    }

    $products.innerHTML = items.map(p => `
        <article class="card product" data-id="${p.id}">
            <div class="product-img" data-open style="background:${bgFor(p.id)}">${productImage(p)}</div>
            <div class="product-body">
                <div class="product-cat">${escapeHtml(p.categoryIcon)} ${escapeHtml(p.categoryName)}</div>
                <div class="product-name" data-open>${escapeHtml(p.name)}</div>
                ${stockLabel(p.stock)}
                <div class="product-footer">
                    <span class="price">${formatPrice(p.price)}</span>
                    <button class="btn btn-sm" data-add ${p.stock === 0 ? 'disabled' : ''}>В корзину</button>
                </div>
            </div>
        </article>`).join('');
}

function renderPagination(total) {
    const pages = Math.ceil(total / PAGE_SIZE);
    if (pages <= 1) { $pagination.innerHTML = ''; return; }

    let html = '';
    for (let i = 1; i <= pages; i++) {
        html += `<button class="btn btn-sm ${i === state.page ? '' : 'btn-outline'}" data-page="${i}">${i}</button>`;
    }
    $pagination.innerHTML = html;
}

$pagination.addEventListener('click', e => {
    const btn = e.target.closest('[data-page]');
    if (!btn) return;
    state.page = Number(btn.dataset.page);
    loadProducts();
    window.scrollTo({ top: 0, behavior: 'smooth' });
});

$products.addEventListener('click', e => {
    const card = e.target.closest('.product');
    if (!card) return;
    const id = Number(card.dataset.id);

    if (e.target.closest('[data-add]')) addToCart(id, 1, e.target.closest('[data-add]'));
    else if (e.target.closest('[data-open]')) openProduct(id);
});

async function addToCart(productId, quantity, button) {
    if (button) button.disabled = true;
    try {
        const cart = await api.cart.add(productId, quantity);
        setCartBadge(cart);
        toast('Товар добавлен в корзину', 'success');
        return true;
    } catch (e) {
        toastError(e);
        return false;
    } finally {
        if (button) button.disabled = false;
    }
}

// ---------- Модальное окно товара ----------

async function openProduct(id) {
    try {
        const p = await api.product(id);
        $modalBody.innerHTML = `
            <button class="modal-close" data-close aria-label="Закрыть">×</button>
            <div class="product-img" style="background:${bgFor(p.id)}">${productImage(p)}</div>
            <div class="modal-content">
                <div class="product-cat">${escapeHtml(p.categoryIcon)} ${escapeHtml(p.categoryName)}</div>
                <h2>${escapeHtml(p.name)}</h2>
                <p>${escapeHtml(p.description)}</p>
                ${stockLabel(p.stock)}
                <div class="modal-actions">
                    <span class="price">${formatPrice(p.price)}</span>
                    <div class="qty" style="margin-left:auto">
                        <button data-qty="-1">−</button>
                        <input type="number" id="modal-qty" value="1" min="1" max="${p.stock}">
                        <button data-qty="1">+</button>
                    </div>
                    <button class="btn" id="modal-add" ${p.stock === 0 ? 'disabled' : ''}>В корзину</button>
                </div>
            </div>`;
        $modal.hidden = false;

        const $qty = document.getElementById('modal-qty');
        const clamp = v => Math.min(Math.max(1, v || 1), Math.max(1, p.stock));

        $modalBody.querySelectorAll('[data-qty]').forEach(btn =>
            btn.addEventListener('click', () => { $qty.value = clamp(Number($qty.value) + Number(btn.dataset.qty)); }));
        $qty.addEventListener('change', () => { $qty.value = clamp(Number($qty.value)); });

        document.getElementById('modal-add').addEventListener('click', async e => {
            if (await addToCart(p.id, clamp(Number($qty.value)), e.currentTarget)) closeModal();
        });
    } catch (e) {
        toastError(e);
    }
}

function closeModal() { $modal.hidden = true; }

$modal.addEventListener('click', e => {
    if (e.target === $modal || e.target.closest('[data-close]')) closeModal();
});
document.addEventListener('keydown', e => { if (e.key === 'Escape') closeModal(); });
