namespace PrintShop.Core.Models;

/// <summary>Дополнительная услуга: ламинирование, резка, переплёт и т.д.</summary>
public class AdditionalService : EntityBase
{
    private string _name = string.Empty;
    private decimal _price;

    public string Name
    {
        get => _name;
        set => _name = RequireText(value, "Название услуги", 100);
    }

    public decimal Price
    {
        get => _price;
        set => _price = RequireNonNegative(value, "Цена услуги");
    }

    public ServiceChargeType ChargeType { get; set; } = ServiceChargeType.PerUnit;

    public string ChargeTypeName => EnumNames.Of(ChargeType);

    /// <summary>Стоимость услуги для позиции с заданным тиражом.</summary>
    public decimal CalculateCost(int quantity) =>
        ChargeType == ServiceChargeType.PerUnit ? Price * quantity : Price;

    public override string ToString() => $"{Name} ({Price:0.00} {ChargeTypeName})";
}
