namespace ShopApp.Models;

public enum OrderStatus
{
    New,        // Новый
    Processing, // В обработке
    Shipped,    // Отправлен
    Delivered,  // Доставлен
    Cancelled   // Отменён
}

/// <summary>Заказ, оформленный из корзины.</summary>
public class Order
{
    public int Id { get; set; }
    public Guid ClientId { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Comment { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.New;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Сумма заказа фиксируется в момент оформления.</summary>
    public decimal Total { get; set; }

    public List<OrderItem> Items { get; set; } = new();
}

/// <summary>
/// Позиция заказа. Название и цена копируются из товара ("снимок"),
/// чтобы изменение цены в каталоге не меняло уже оформленные заказы.
/// </summary>
public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}
