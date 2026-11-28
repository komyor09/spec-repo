namespace ShopApp.Models;

/// <summary>Товар каталога.</summary>
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }

    /// <summary>Остаток на складе.</summary>
    public int Stock { get; set; }

    public string? ImageUrl { get; set; }

    /// <summary>
    /// Мягкое удаление: скрытый товар не показывается в каталоге,
    /// но остаётся в БД, чтобы не ломать историю заказов.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}
