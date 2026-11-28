// Общие функции для всех страниц: форматирование, уведомления, счётчик корзины.
import { api } from './api.js';

export const CURRENCY = 'сум';

const priceFormat = new Intl.NumberFormat('ru-RU', { maximumFractionDigits: 2 });
export const formatPrice = value => `${priceFormat.format(value)} ${CURRENCY}`;

export const formatDate = iso => new Date(iso).toLocaleString('ru-RU', {
    day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit',
});

export const STATUS_LABELS = {
    New: 'Новый',
    Processing: 'В обработке',
    Shipped: 'Отправлен',
    Delivered: 'Доставлен',
    Cancelled: 'Отменён',
};

export const statusBadge = status =>
    `<span class="status status-${status}">${STATUS_LABELS[status] ?? status}</span>`;

/** Экранирование текста перед вставкой в innerHTML (защита от XSS). */
export function escapeHtml(value) {
    return String(value ?? '')
        .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;').replace(/'/g, '&#39;');
}

/** Склонение: plural(3, ['товар','товара','товаров']) → 'товара' */
export function plural(n, [one, few, many]) {
    const n10 = n % 10, n100 = n % 100;
    if (n10 === 1 && n100 !== 11) return one;
    if (n10 >= 2 && n10 <= 4 && (n100 < 12 || n100 > 14)) return few;
    return many;
}

/** Цветной фон-заглушка для товара без картинки (цвет зависит от id). */
const PALETTE = ['#e8f0ff', '#e4f6ec', '#fdf3e1', '#efe8ff', '#fdeaea', '#e3f4f8'];
export const bgFor = id => PALETTE[id % PALETTE.length];

/** Содержимое блока картинки: <img> если есть ссылка, иначе эмодзи категории. */
export function productImage(item) {
    if (item.imageUrl) return `<img src="${escapeHtml(item.imageUrl)}" alt="" loading="lazy">`;
    return escapeHtml(item.categoryIcon ?? '📦');
}

/** Шапка сайта (одинаковая на всех страницах). active: 'catalog' | 'cart' | 'orders' | 'admin' */
export function renderHeader(active) {
    const link = (key, href, icon, label, extra = '') =>
        `<a href="${href}" class="${active === key ? 'active' : ''}">${icon} <span class="label">${label}</span>${extra}</a>`;

    document.getElementById('header').innerHTML = `
        <div class="container">
            <a href="/" class="logo">🛍️ Shop<span>App</span></a>
            <nav class="nav">
                ${link('catalog', '/', '🏬', 'Каталог')}
                ${link('cart', '/cart.html', '🛒', 'Корзина', ' <span class="badge" id="cart-count" data-count="0"></span>')}
                ${link('orders', '/orders.html', '📦', 'Мои заказы')}
                ${link('admin', '/admin.html', '⚙️', 'Админ')}
            </nav>
        </div>`;
}

// ---------- Уведомления ----------

function toastContainer() {
    let el = document.querySelector('.toasts');
    if (!el) {
        el = document.createElement('div');
        el.className = 'toasts';
        document.body.append(el);
    }
    return el;
}

export function toast(message, type = 'info') {
    const el = document.createElement('div');
    el.className = `toast ${type}`;
    el.textContent = message;
    toastContainer().append(el);
    setTimeout(() => el.remove(), 3500);
}

export const toastError = err => toast(err?.message ?? String(err), 'error');

// ---------- Счётчик корзины в шапке ----------

export function setCartBadge(cart) {
    const badge = document.getElementById('cart-count');
    if (!badge) return;
    const count = cart?.totalQuantity ?? 0;
    badge.textContent = count;
    badge.dataset.count = count;
}

export async function refreshCartBadge() {
    try {
        setCartBadge(await api.cart.get());
    } catch {
        /* счётчик не критичен — молча пропускаем */
    }
}

// ---------- Заказы ----------

/** Таблица позиций заказа (используется на странице заказов и в админке). */
export function renderOrderItems(order) {
    return `
        <table class="order-items">
            ${order.items.map(i => `
                <tr>
                    <td>${escapeHtml(i.productName)}</td>
                    <td class="muted">${i.quantity} × ${formatPrice(i.unitPrice)}</td>
                    <td>${formatPrice(i.lineTotal)}</td>
                </tr>`).join('')}
        </table>`;
}
