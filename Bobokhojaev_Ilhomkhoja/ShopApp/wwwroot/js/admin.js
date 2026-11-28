import { api, adminKey } from './api.js';
import {
    renderHeader, refreshCartBadge, formatPrice, formatDate, escapeHtml,
    statusBadge, renderOrderItems, STATUS_LABELS, toast, toastError,
} from './common.js';

const $login = document.getElementById('login');
const $panel = document.getElementById('panel');
const $orders = document.getElementById('admin-orders');
const $products = document.getElementById('admin-products');
const $productForm = document.getElementById('product-form');

let products = [];     // кэш товаров для редактирования
let categoriesLoaded = false;

renderHeader('admin');
refreshCartBadge();
start();

// ===================== Вход / выход =====================

async function start() {
    if (adminKey.get()) {
        try {
            await api.admin.check();
            return showPanel();
        } catch {
            adminKey.clear();
        }
    }
    $login.hidden = false;
}

document.getElementById('login-form').addEventListener('submit', async e => {
    e.preventDefault();
    const errorBox = document.getElementById('login-error');
    errorBox.textContent = '';
    adminKey.set(document.getElementById('admin-key').value);
    try {
        await api.admin.check();
        $login.hidden = true;
        showPanel();
    } catch (err) {
        adminKey.clear();
        errorBox.textContent = err.message;
    }
});

document.getElementById('logout').addEventListener('click', () => {
    adminKey.clear();
    location.reload();
});

function showPanel() {
    $panel.hidden = false;
    loadOrders();
}

/** Если ключ перестал подходить (401) — возвращаем на экран входа. */
function handleError(err) {
    if (err.status === 401) {
        adminKey.clear();
        location.reload();
        return;
    }
    toastError(err);
}

// ===================== Вкладки =====================

document.querySelector('.tabs').addEventListener('click', e => {
    const tab = e.target.closest('[data-tab]');
    if (!tab) return;
    document.querySelectorAll('[data-tab]').forEach(t => t.classList.toggle('active', t === tab));
    document.querySelectorAll('[data-pane]').forEach(p => { p.hidden = p.dataset.pane !== tab.dataset.tab; });
    if (tab.dataset.tab === 'orders') loadOrders();
    else loadProducts();
});

// ===================== Заказы =====================

const $statusFilter = document.getElementById('status-filter');
$statusFilter.addEventListener('change', loadOrders);
document.getElementById('reload-orders').addEventListener('click', loadOrders);

async function loadOrders() {
    $orders.innerHTML = '<div class="loader">Загрузка…</div>';
    try {
        const orders = await api.admin.orders($statusFilter.value || undefined);
        $orders.innerHTML = orders.length
            ? orders.map(renderOrder).join('')
            : '<div class="card empty"><div class="icon">📭</div>Заказов нет</div>';
    } catch (err) {
        $orders.innerHTML = '';
        handleError(err);
    }
}

function renderOrder(o) {
    const buttons = o.nextStatuses.map(s => `
        <button class="btn btn-sm ${s === 'Cancelled' ? 'btn-danger' : 'btn-outline'}"
                data-order="${o.id}" data-status="${s}">
            → ${STATUS_LABELS[s]}
        </button>`).join('');

    return `
        <div class="card order">
            <div class="order-head">
                <span class="num">№${o.id}</span>
                ${statusBadge(o.status)}
                <span class="date">${formatDate(o.createdAt)}</span>
                <span class="total">${formatPrice(o.total)}</span>
            </div>
            ${renderOrderItems(o)}
            <div class="order-meta">
                👤 ${escapeHtml(o.customerName)} · 📞 ${escapeHtml(o.phone)} · ✉️ ${escapeHtml(o.email)}<br>
                📍 ${escapeHtml(o.address)}
                ${o.comment ? `<br>💬 ${escapeHtml(o.comment)}` : ''}
            </div>
            ${buttons ? `<div class="order-actions"><span style="color:var(--muted);font-size:14px">Сменить статус:</span>${buttons}</div>` : ''}
        </div>`;
}

$orders.addEventListener('click', async e => {
    const btn = e.target.closest('[data-status]');
    if (!btn) return;
    const { order, status } = btn.dataset;
    if (status === 'Cancelled' && !confirm(`Отменить заказ №${order}? Товары вернутся на склад.`)) return;

    btn.disabled = true;
    try {
        await api.admin.setStatus(Number(order), status);
        toast(`Заказ №${order}: ${STATUS_LABELS[status]}`, 'success');
        loadOrders();
    } catch (err) {
        btn.disabled = false;
        handleError(err);
    }
});

// ===================== Товары =====================

async function loadProducts() {
    try {
        if (!categoriesLoaded) {
            const categories = await api.categories();
            $productForm.elements.categoryId.innerHTML = categories
                .map(c => `<option value="${c.id}">${escapeHtml(c.icon)} ${escapeHtml(c.name)}</option>`).join('');
            categoriesLoaded = true;
        }
        const result = await api.admin.products();
        products = result.items;
        renderProducts();
    } catch (err) {
        handleError(err);
    }
}

function renderProducts() {
    $products.innerHTML = products.map(p => `
        <tr class="${p.isActive ? '' : 'inactive'}">
            <td>${p.id}</td>
            <td>${escapeHtml(p.name)}</td>
            <td>${escapeHtml(p.categoryIcon)} ${escapeHtml(p.categoryName)}</td>
            <td>${formatPrice(p.price)}</td>
            <td style="color:${p.stock === 0 ? 'var(--danger)' : 'inherit'}">${p.stock}</td>
            <td>${p.isActive ? 'В каталоге' : 'Скрыт'}</td>
            <td style="white-space:nowrap">
                <button class="btn btn-outline btn-sm" data-edit="${p.id}">✏️</button>
                <button class="btn btn-outline btn-sm" data-toggle="${p.id}">${p.isActive ? 'Скрыть' : 'Показать'}</button>
            </td>
        </tr>`).join('');
}

const toDto = p => ({
    name: p.name,
    description: p.description,
    price: p.price,
    stock: p.stock,
    imageUrl: p.imageUrl,
    categoryId: p.categoryId,
    isActive: p.isActive,
});

$products.addEventListener('click', async e => {
    const editBtn = e.target.closest('[data-edit]');
    const toggleBtn = e.target.closest('[data-toggle]');

    if (editBtn) {
        fillForm(products.find(p => p.id === Number(editBtn.dataset.edit)));
    } else if (toggleBtn) {
        const p = products.find(x => x.id === Number(toggleBtn.dataset.toggle));
        try {
            await api.admin.updateProduct(p.id, { ...toDto(p), isActive: !p.isActive });
            loadProducts();
        } catch (err) {
            handleError(err);
        }
    }
});

function fillForm(p) {
    const f = $productForm.elements;
    f.id.value = p.id;
    f.name.value = p.name;
    f.categoryId.value = p.categoryId;
    f.price.value = p.price;
    f.stock.value = p.stock;
    f.imageUrl.value = p.imageUrl ?? '';
    f.description.value = p.description;
    f.isActive.checked = p.isActive;

    document.getElementById('product-form-title').textContent = `Редактирование товара №${p.id}`;
    document.getElementById('product-submit').textContent = 'Сохранить';
    document.getElementById('product-cancel').hidden = false;
    $productForm.scrollIntoView({ behavior: 'smooth' });
}

function resetForm() {
    $productForm.reset();
    $productForm.elements.id.value = '';
    document.getElementById('product-form-title').textContent = 'Новый товар';
    document.getElementById('product-submit').textContent = 'Добавить товар';
    document.getElementById('product-cancel').hidden = true;
    document.getElementById('product-error').textContent = '';
}

document.getElementById('product-cancel').addEventListener('click', resetForm);

$productForm.addEventListener('submit', async e => {
    e.preventDefault();
    const f = $productForm.elements;
    const errorBox = document.getElementById('product-error');
    errorBox.textContent = '';

    const dto = {
        name: f.name.value.trim(),
        description: f.description.value.trim(),
        price: Number(f.price.value),
        stock: Number(f.stock.value),
        imageUrl: f.imageUrl.value.trim() || null,
        categoryId: Number(f.categoryId.value),
        isActive: f.isActive.checked,
    };

    try {
        if (f.id.value) {
            await api.admin.updateProduct(Number(f.id.value), dto);
            toast('Товар сохранён', 'success');
        } else {
            await api.admin.createProduct(dto);
            toast('Товар добавлен', 'success');
        }
        resetForm();
        loadProducts();
    } catch (err) {
        if (err.status === 401) return handleError(err);
        errorBox.textContent = err.message;
    }
});
