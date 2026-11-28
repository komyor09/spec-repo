// Обёртка над fetch для работы с API магазина.

const CLIENT_ID_KEY = 'shop.clientId';
const ADMIN_KEY = 'shop.adminKey';

/** ID покупателя: генерируется один раз и хранится в localStorage. */
export function getClientId() {
    let id = localStorage.getItem(CLIENT_ID_KEY);
    if (!id) {
        id = (crypto.randomUUID && window.isSecureContext)
            ? crypto.randomUUID()
            : 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, c => {
                const r = Math.random() * 16 | 0;
                return (c === 'x' ? r : (r & 0x3 | 0x8)).toString(16);
            });
        localStorage.setItem(CLIENT_ID_KEY, id);
    }
    return id;
}

export const adminKey = {
    get: () => sessionStorage.getItem(ADMIN_KEY),
    set: key => sessionStorage.setItem(ADMIN_KEY, key),
    clear: () => sessionStorage.removeItem(ADMIN_KEY),
};

export class ApiError extends Error {
    constructor(message, status) {
        super(message);
        this.status = status;
    }
}

/** Достаёт текст ошибки из ответа: { error } от нашего middleware или ValidationProblemDetails от ASP.NET. */
function extractError(data, status) {
    if (data?.error) return data.error;
    if (data?.errors) return Object.values(data.errors).flat().join(' ');
    return data?.title || `Ошибка сервера (${status})`;
}

async function request(method, url, body, { admin = false } = {}) {
    const headers = { 'X-Client-Id': getClientId() };
    if (body !== undefined) headers['Content-Type'] = 'application/json';
    if (admin) headers['X-Admin-Key'] = adminKey.get() ?? '';

    let response;
    try {
        response = await fetch(url, {
            method,
            headers,
            body: body !== undefined ? JSON.stringify(body) : undefined,
        });
    } catch {
        throw new ApiError('Нет связи с сервером', 0);
    }

    if (response.status === 204) return null;

    const data = await response.json().catch(() => null);
    if (!response.ok) throw new ApiError(extractError(data, response.status), response.status);
    return data;
}

function query(params) {
    const qs = new URLSearchParams();
    for (const [k, v] of Object.entries(params)) {
        if (v !== undefined && v !== null && v !== '') qs.append(k, v);
    }
    const s = qs.toString();
    return s ? `?${s}` : '';
}

export const api = {
    categories: () => request('GET', '/api/categories'),
    products: params => request('GET', '/api/products' + query(params)),
    product: id => request('GET', `/api/products/${id}`),

    cart: {
        get: () => request('GET', '/api/cart'),
        add: (productId, quantity = 1) => request('POST', '/api/cart/items', { productId, quantity }),
        setQuantity: (productId, quantity) => request('PUT', `/api/cart/items/${productId}`, { quantity }),
        remove: productId => request('DELETE', `/api/cart/items/${productId}`),
        clear: () => request('DELETE', '/api/cart'),
    },

    orders: {
        create: checkout => request('POST', '/api/orders', checkout),
        list: () => request('GET', '/api/orders'),
        get: id => request('GET', `/api/orders/${id}`),
        cancel: id => request('POST', `/api/orders/${id}/cancel`),
    },

    admin: {
        check: () => request('GET', '/api/admin/check', undefined, { admin: true }),
        orders: status => request('GET', '/api/admin/orders' + query({ status }), undefined, { admin: true }),
        setStatus: (id, status) => request('PUT', `/api/admin/orders/${id}/status`, { status }, { admin: true }),
        products: search => request('GET', '/api/admin/products' + query({ search }), undefined, { admin: true }),
        createProduct: dto => request('POST', '/api/admin/products', dto, { admin: true }),
        updateProduct: (id, dto) => request('PUT', `/api/admin/products/${id}`, dto, { admin: true }),
    },
};
