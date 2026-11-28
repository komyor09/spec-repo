using System.ComponentModel.DataAnnotations;
using ShopApp.Models;

namespace ShopApp.Dtos;

// ===================== Каталог =====================

public record CategoryDto(int Id, string Name, string Icon);

public record ProductDto(
    int Id,
    string Name,
    string Description,
    decimal Price,
    int Stock,
    string? ImageUrl,
    bool IsActive,
    int CategoryId,
    string CategoryName,
    string CategoryIcon);

public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

/// <summary>Создание / редактирование товара (админка).</summary>
public class ProductEditDto
{
    [Required(ErrorMessage = "Укажите название")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Название: от 2 до 200 символов")]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 1_000_000_000.0, ErrorMessage = "Цена должна быть больше нуля")]
    public decimal Price { get; set; }

    [Range(0, 100_000, ErrorMessage = "Остаток: от 0 до 100 000")]
    public int Stock { get; set; }

    [StringLength(500)]
    public string? ImageUrl { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Выберите категорию")]
    public int CategoryId { get; set; }

    public bool IsActive { get; set; } = true;
}

// ===================== Корзина =====================

public record CartItemDto(
    int ProductId,
    string Name,
    decimal Price,
    int Quantity,
    int Stock,
    bool IsAvailable,
    string? ImageUrl,
    string CategoryIcon,
    decimal LineTotal);

public record CartDto(IReadOnlyList<CartItemDto> Items, int TotalQuantity, decimal Total);

public class AddToCartDto
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Range(1, 999, ErrorMessage = "Количество: от 1 до 999")]
    public int Quantity { get; set; } = 1;
}

public class UpdateQuantityDto
{
    /// <summary>0 — удалить позицию.</summary>
    [Range(0, 999, ErrorMessage = "Количество: от 0 до 999")]
    public int Quantity { get; set; }
}

// ===================== Заказы =====================

public class CheckoutDto
{
    [Required(ErrorMessage = "Укажите имя")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Имя: от 2 до 100 символов")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите телефон")]
    [RegularExpression(@"^\+?[0-9\s\-()]{7,20}$", ErrorMessage = "Некорректный номер телефона")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите e-mail")]
    [EmailAddress(ErrorMessage = "Некорректный e-mail")]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите адрес доставки")]
    [StringLength(300, MinimumLength = 5, ErrorMessage = "Адрес: от 5 до 300 символов")]
    public string Address { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Комментарий: до 500 символов")]
    public string? Comment { get; set; }
}

public record OrderItemDto(int? ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal LineTotal);

public record OrderDto(
    int Id,
    OrderStatus Status,
    DateTime CreatedAt,
    string CustomerName,
    string Phone,
    string Email,
    string Address,
    string? Comment,
    decimal Total,
    IReadOnlyList<OrderItemDto> Items,
    bool CanCancel,
    IReadOnlyList<OrderStatus> NextStatuses);

public class ChangeStatusDto
{
    [Required(ErrorMessage = "Укажите статус")]
    public OrderStatus? Status { get; set; }
}
