namespace PrintShop.Core.Models;

/// <summary>Расходный материал (бумага, плёнка, баннерная ткань) и его остаток на складе.</summary>
public class Material : EntityBase
{
    private string _name = string.Empty;
    private string _format = PaperFormats.A3;
    private int _density;
    private decimal _price;
    private decimal _quantity;
    private decimal _minQuantity;

    public string Name
    {
        get => _name;
        set => _name = RequireText(value, "Название материала", 150);
    }

    /// <summary>Плотность, г/м².</summary>
    public int Density
    {
        get => _density;
        set
        {
            if (value < 0 || value > 2000)
                throw new OrderValidationException("Плотность должна быть в диапазоне 0–2000 г/м².", nameof(Density));
            _density = value;
        }
    }

    /// <summary>Формат листа (A4, A3, SRA3...) или «Рулон» для рулонных материалов.</summary>
    public string Format
    {
        get => _format;
        set
        {
            var v = RequireText(value, "Формат", 20);
            if (!PaperFormats.IsKnown(v))
                throw new OrderValidationException($"Неизвестный формат материала «{v}».", nameof(Format));
            _format = v;
        }
    }

    /// <summary>Цена за единицу (лист или м²).</summary>
    public decimal Price
    {
        get => _price;
        set => _price = RequireNonNegative(value, "Цена");
    }

    /// <summary>Остаток на складе (в листах или м²).</summary>
    public decimal Quantity
    {
        get => _quantity;
        set => _quantity = RequireNonNegative(value, "Остаток");
    }

    /// <summary>Минимальный запас, ниже которого материал считается дефицитным.</summary>
    public decimal MinQuantity
    {
        get => _minQuantity;
        set => _minQuantity = RequireNonNegative(value, "Минимальный запас");
    }

    public bool IsRoll => Format == PaperFormats.Roll;

    public string Unit => IsRoll ? "м²" : "лист";

    public bool IsBelowMinimum => Quantity < MinQuantity;

    /// <summary>Списание материала со склада.</summary>
    public void WriteOff(decimal amount)
    {
        if (amount < 0)
            throw new OrderValidationException("Количество списания не может быть отрицательным.");
        if (amount > Quantity)
            throw new OrderValidationException(
                $"Недостаточно материала «{Name}» на складе: требуется {amount:0.##} {Unit}, в наличии {Quantity:0.##} {Unit}.");
        Quantity -= amount;
    }

    public override string ToString() => Density > 0 ? $"{Name}, {Density} г/м², {Format}" : $"{Name}, {Format}";
}
