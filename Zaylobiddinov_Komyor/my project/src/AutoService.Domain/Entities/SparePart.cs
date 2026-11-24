using AutoService.Domain.Common;
using AutoService.Domain.Exceptions;

namespace AutoService.Domain.Entities;

/// <summary>Запасная часть на складе. Остаток меняется только через методы.</summary>
public class SparePart : Entity
{
    public string Article { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int Stock { get; private set; }

    protected SparePart() { }

    public SparePart(string article, string name, decimal price, int stock = 0)
    {
        Article = Guard.NotEmpty(article, "Артикул").ToUpperInvariant();
        Name = Guard.NotEmpty(name, "Наименование");
        ChangePrice(price);
        if (stock < 0) throw new DomainException("Остаток не может быть отрицательным.");
        Stock = stock;
    }

    public void ChangePrice(decimal price) => Price = Guard.Positive(price, "Цена");

    /// <summary>Приход запчастей на склад.</summary>
    public void Receive(int quantity) => Stock += Guard.Positive(quantity, "Количество");

    /// <summary>Списание со склада. При нехватке — исключение NotEnoughStockException.</summary>
    public void WriteOff(int quantity)
    {
        Guard.Positive(quantity, "Количество");
        if (quantity > Stock)
            throw new NotEnoughStockException(Name, quantity, Stock);
        Stock -= quantity;
    }

    public override string ToString() => $"{Article} {Name}";
}
