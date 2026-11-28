using WarehouseAccounting.Exceptions;

namespace WarehouseAccounting.Models;

/// <summary>
/// Базовый класс товара. Хранит общие данные и правила их изменения.
/// Все наследники обязаны указать свою категорию.
/// </summary>
public abstract class Item
{
    private static int _nextId = 1;

    private decimal _price;

    protected Item(string name, decimal price, int quantity)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Название не может быть пустым.", nameof(name));
        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Количество не может быть отрицательным.");

        Id = _nextId++;
        Name = name.Trim();
        Price = price;
        Quantity = quantity;
    }

    public int Id { get; }

    public string Name { get; }

    public decimal Price
    {
        get => _price;
        set => _price = value >= 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), "Цена не может быть отрицательной.");
    }

    public int Quantity { get; private set; }

    public decimal TotalValue => Price * Quantity;

    /// <summary>Категория товара — каждый наследник задаёт свою.</summary>
    public abstract string Category { get; }

    public void Increase(int amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Количество должно быть больше нуля.");

        Quantity += amount;
    }

    public void Decrease(int amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Количество должно быть больше нуля.");
        if (amount > Quantity)
            throw new InsufficientStockException(Name, Quantity, amount);

        Quantity -= amount;
    }

    /// <summary>Описание товара. Наследники могут дополнять его.</summary>
    public virtual string GetDescription() => $"[{Id}] {Name} ({Category})";

    public override string ToString() =>
        $"{GetDescription()} — {Quantity} шт. × {Price:F2} = {TotalValue:F2}";
}