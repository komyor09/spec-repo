namespace WarehouseAccounting.Models;

/// <summary>Одежда — имеет размер.</summary>
public class Clothing : Item
{
    public Clothing(string name, decimal price, int quantity, string size)
        : base(name, price, quantity)
    {
        if (string.IsNullOrWhiteSpace(size))
            throw new ArgumentException("Размер не может быть пустым.", nameof(size));

        Size = size.Trim().ToUpperInvariant();
    }

    public string Size { get; }

    public override string Category => "Одежда";

    public override string GetDescription() => $"{base.GetDescription()}, размер {Size}";
}