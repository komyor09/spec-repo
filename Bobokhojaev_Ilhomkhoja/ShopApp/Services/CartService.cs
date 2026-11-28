using Microsoft.EntityFrameworkCore;
using ShopApp.Data;
using ShopApp.Dtos;
using ShopApp.Infrastructure;
using ShopApp.Models;

namespace ShopApp.Services;

/// <summary>Корзина хранится в БД и привязана к ClientId покупателя.</summary>
public class CartService(ShopDbContext db)
{
    public async Task<CartDto> GetAsync(Guid clientId) => (await LoadAsync(clientId)).ToDto();

    public async Task<CartDto> AddItemAsync(Guid clientId, int productId, int quantity)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == productId && p.IsActive)
                      ?? throw ShopException.NotFound("Товар не найден");

        var cart = await GetOrCreateAsync(clientId);
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);

        var newQuantity = (item?.Quantity ?? 0) + quantity;
        EnsureStock(product, newQuantity);

        if (item is null)
            cart.Items.Add(new CartItem { ProductId = productId, Product = product, Quantity = quantity });
        else
            item.Quantity = newQuantity;

        cart.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return await GetAsync(clientId);
    }

    public async Task<CartDto> SetQuantityAsync(Guid clientId, int productId, int quantity)
    {
        var cart = await LoadAsync(clientId) ?? throw ShopException.NotFound("Корзина пуста");
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId)
                   ?? throw ShopException.NotFound("Товара нет в корзине");

        if (quantity == 0)
        {
            db.CartItems.Remove(item);
        }
        else
        {
            EnsureStock(item.Product, quantity);
            item.Quantity = quantity;
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return await GetAsync(clientId);
    }

    public Task<CartDto> RemoveItemAsync(Guid clientId, int productId) =>
        SetQuantityAsync(clientId, productId, 0);

    public async Task ClearAsync(Guid clientId)
    {
        var cart = await LoadAsync(clientId);
        if (cart is null) return;

        db.CartItems.RemoveRange(cart.Items);
        cart.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    private Task<Cart?> LoadAsync(Guid clientId) =>
        db.Carts
            .Include(c => c.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Category)
            .FirstOrDefaultAsync(c => c.ClientId == clientId);

    private async Task<Cart> GetOrCreateAsync(Guid clientId)
    {
        var cart = await LoadAsync(clientId);
        if (cart is null)
        {
            cart = new Cart { ClientId = clientId };
            db.Carts.Add(cart);
        }
        return cart;
    }

    private static void EnsureStock(Product product, int quantity)
    {
        if (!product.IsActive)
            throw ShopException.BadRequest($"Товар «{product.Name}» больше не продаётся");
        if (quantity > product.Stock)
            throw ShopException.BadRequest(product.Stock == 0
                ? $"Товара «{product.Name}» нет в наличии"
                : $"Недостаточно товара «{product.Name}» на складе. Доступно: {product.Stock} шт.");
    }
}
