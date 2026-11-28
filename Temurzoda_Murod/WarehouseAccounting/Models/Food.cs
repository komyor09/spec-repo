namespace WarehouseAccounting.Models;

/// <summary>Продукты — имеют срок годности.</summary>
public class Food : Item
{
    public Food(string name, decimal price, int quantity, DateTime expirationDate)
        : base(name, price, quantity)
    {
        ExpirationDate = expirationDate.Date;
    }

    public DateTime ExpirationDate { get; }

    public override string Category => "Продукты";

    public bool IsExpired(DateTime date) => date.Date > ExpirationDate;

    public override string GetDescription() =>
        $"{base.GetDescription()}, годен до {ExpirationDate:dd.MM.yyyy}";
}