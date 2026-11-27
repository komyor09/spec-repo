namespace PrintShop.Core.Models;

/// <summary>Вид продукции из справочника (прайс-лист): базовая цена и единица измерения.</summary>
public class ProductType : EntityBase
{
    private string _name = string.Empty;
    private string _unit = "шт";
    private decimal _basePrice;

    public string Name
    {
        get => _name;
        set => _name = RequireText(value, "Название вида продукции", 100);
    }

    /// <summary>Какой класс-наследник PrintProduct соответствует этому виду.</summary>
    public ProductKind Kind { get; set; }

    /// <summary>Базовая цена печати (смысл зависит от вида — см. описание в классе продукции).</summary>
    public decimal BasePrice
    {
        get => _basePrice;
        set => _basePrice = RequireNonNegative(value, "Базовая цена");
    }

    public string Unit
    {
        get => _unit;
        set => _unit = RequireText(value, "Единица измерения", 20);
    }

    public string KindName => EnumNames.Of(Kind);

    public override string ToString() => Name;
}
