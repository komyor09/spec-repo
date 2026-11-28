import { api } from './api.js';
import {
    renderHeader, refreshCartBadge, formatPrice, formatDate, escapeHtml,
    statusBadge, renderOrderItems, toast, toastError,
} from './common.js';

const $orders = document.getElementById('orders');
const createdId = Number(new URLSearchParams(location.search).get('created')) || null;

renderHeader('orders');
refreshCartBadge();
load();

async function load() {
    try {
        render(await api.orders.list());
    } catch (e) {
        $orders.innerHTML = '';
        toastError(e);
    }
}

function render(orders) {
    if (orders.length === 0) {
        $orders.innerHTML = `
            <div class="card empty">
                <div class="icon">📦</div>
                <p>У вас пока нет заказов</p>
                <a href="/" class="btn">Перейти в каталог</a>
            </div>`;
        return;
    }

    $orders.innerHTML = orders.map(renderOrder).join('');

    const created = document.querySelector(`.order[data-id="${createdId}"]`);
    if (created) created.scrollIntoView({ behavior: 'smooth', block: 'center' });
}

function renderOrder(order) {
    return `
        <div class="card order ${order.id === createdId ? 'highlight' : ''}" data-id="${order.id}">
            <div class="order-head">
                <span class="num">Заказ №${order.id}</span>
                ${statusBadge(order.status)}
                <span class="date">${formatDate(order.createdAt)}</span>
                <span class="total">${formatPrice(order.total)}</span>
            </div>
            ${renderOrderItems(order)}
            <div class="order-meta">
                📍 ${escapeHtml(order.address)} · 📞 ${escapeHtml(order.phone)}
                ${order.comment ? `<br>💬 ${escapeHtml(order.comment)}` : ''}
            </div>
            ${order.canCancel ? `
                <div class="order-actions">
                    <button class="btn btn-danger btn-sm" data-cancel="${order.id}">Отменить заказ</button>
                </div>` : ''}
        </div>`;
}

$orders.addEventListener('click', async e => {
    const btn = e.target.closest('[data-cancel]');
    if (!btn) return;
    const id = Number(btn.dataset.cancel);
    if (!confirm(`Отменить заказ №${id}?`)) return;

    btn.disabled = true;
    try {
        await api.orders.cancel(id);
        toast(`Заказ №${id} отменён`, 'success');
        load();
    } catch (err) {
        toastError(err);
        btn.disabled = false;
    }
});
