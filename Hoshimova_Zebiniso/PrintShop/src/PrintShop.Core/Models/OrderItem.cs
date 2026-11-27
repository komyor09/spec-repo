using PrintShop.Core.Models.Products;

namespace PrintShop.Core.Models;

/// <summary>Позиция заказа: изделие, количество и рассчитанная стоимость.</summary>
public class OrderItem : EntityBase
{
    private decimal _price;
    private decimal _discount;

    public OrderItem(PrintProduct product)
    {
        Product = product ?? throw new ArgumentNullException(nameof(product));
    }

    public int OrderId { get; set; }

    /// <summary>Изделие (полиморфная ссылка на конкретный вид продукции).</summary>
    public PrintProduct Product { get; }

    public int Quantity => Product.Quantity;

    /// <summary>Итоговая стоимость позиции с учётом скидки.</summary>
    public decimal Price
    {
        get => _price;
        set => _price = RequireNonNegative(value, "Стоимость позиции");
    }

    /// <summary>Сумма скидки за тираж по позиции.</summary>
    public decimal Discount
    {
        get => _discount;
        set => _discount = RequireNonNegative(value, "Скидка");
    }

    public string Description => Product.Description;

    public string ServicesText => Product.Services.Count == 0
        ? "—"
        : string.Join(", ", Product.Services.Select(s => s.Name));

    public decimal UnitPrice => Quantity == 0 ? 0 : Math.Round(Price / Quantity, 2);
}
