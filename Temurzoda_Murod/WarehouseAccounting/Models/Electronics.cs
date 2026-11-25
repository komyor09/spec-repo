namespace WarehouseAccounting.Models;

/// <summary>Электроника — дополнительно имеет гарантию.</summary>
public class Electronics : Item
{
    public Electronics(string name, decimal price, int quantity, int warrantyMonths)
        : base(name, price, quantity)
    {
        if (warrantyMonths < 0)
            throw new ArgumentOutOfRangeException(nameof(warrantyMonths), "Гарантия не может быть отрицательной.");

        WarrantyMonths = warrantyMonths;
    }

    public int WarrantyMonths { get; }

    public override string Category => "Электроника";

    public override string GetDescription() => $"{base.GetDescription()}, гарантия {WarrantyMonths} мес.";
}