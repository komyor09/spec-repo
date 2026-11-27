using PrintShop.Core.Models;
using PrintShop.Core.Models.Products;

namespace PrintShop.Core.Services;

/// <summary>Результат расчёта стоимости одной позиции.</summary>
public record ItemPriceBreakdown(
    decimal PrintAndMaterial,
    decimal Services,
    decimal Subtotal,
    decimal DiscountPercent,
    decimal DiscountAmount,
    decimal Total);

/// <summary>Интерфейс расчёта стоимости заказа (абстракция для подмены в тестах).</summary>
public interface IPriceCalculator
{
    /// <summary>Процент скидки за тираж.</summary>
    decimal GetVolumeDiscountPercent(int quantity);

    /// <summary>Подробный расчёт стоимости одного изделия.</summary>
    ItemPriceBreakdown CalculateItem(PrintProduct product);

    /// <summary>Пересчитывает все позиции заказа, сумму и общую скидку.</summary>
    void Recalculate(Order order);
}

/// <summary>
/// Калькулятор стоимости: стоимость печати и материала берётся из полиморфного метода
/// PrintProduct.CalculatePrice(), к ней добавляются доп. услуги, затем применяется скидка за тираж.
/// </summary>
public class PriceCalculator : IPriceCalculator
{
    /// <summary>Шкала скидок: (минимальный тираж, процент скидки). Отсортирована по убыванию.</summary>
    private readonly (int MinQuantity, decimal Percent)[] _discountScale;

    public PriceCalculator() : this(new[] { (5000, 15m), (1000, 10m), (500, 5m) }) { }

    public PriceCalculator(IEnumerable<(int MinQuantity, decimal Percent)> scale)
    {
        _discountScale = scale.OrderByDescending(s => s.MinQuantity).ToArray();
    }

    public IReadOnlyList<(int MinQuantity, decimal Percent)> DiscountScale => _discountScale;

    public decimal GetVolumeDiscountPercent(int quantity) =>
        _discountScale.FirstOrDefault(s => quantity >= s.MinQuantity).Percent;

    public ItemPriceBreakdown CalculateItem(PrintProduct product)
    {
        ArgumentNullException.ThrowIfNull(product);
        decimal printAndMaterial = Round(product.CalculatePrice()); // полиморфный вызов
        decimal services = Round(product.CalculateServicesCost());
        decimal subtotal = printAndMaterial + services;
        decimal percent = GetVolumeDiscountPercent(product.Quantity);
        decimal discount = Round(subtotal * percent / 100m);
        return new ItemPriceBreakdown(printAndMaterial, services, subtotal, percent, discount, subtotal - discount);
    }

    public void Recalculate(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        foreach (var item in order.Items)
        {
            var b = CalculateItem(item.Product);
            item.Price = b.Total;
            item.Discount = b.DiscountAmount;
        }
        order.Discount = order.Items.Sum(i => i.Discount);
        order.TotalPrice = order.Items.Sum(i => i.Price);
    }

    private static decimal Round(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
}
