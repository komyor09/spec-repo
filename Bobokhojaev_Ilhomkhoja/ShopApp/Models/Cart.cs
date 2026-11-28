namespace ShopApp.Models;

/// <summary>
/// Корзина покупателя. Покупатель идентифицируется ClientId —
/// GUID, который фронтенд генерирует и хранит в localStorage.
/// </summary>
public class Cart
{
    public int Id { get; set; }
    public Guid ClientId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<CartItem> Items { get; set; } = new();
}

/// <summary>Позиция в корзине.</summary>
public class CartItem
{
    public int Id { get; set; }

    public int CartId { get; set; }
    public Cart Cart { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }
}
