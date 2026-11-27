using System.Globalization;

namespace PrintShop.Core.Models.Products;

/// <summary>
/// Абстрактный базовый класс печатной продукции.
/// Содержит общие свойства (тираж, формат, материал, цветность, доп. услуги)
/// и абстрактный метод расчёта стоимости, который каждый вид продукции
/// переопределяет по своим правилам (полиморфизм).
/// </summary>
public abstract class PrintProduct
{
    private int _quantity = 1;
    private string _format = PaperFormats.A4;
    private ProductType _productType;
    private Material _material;
    private readonly List<AdditionalService> _services = new();

    protected PrintProduct(ProductType productType, Material material)
    {
        _productType = productType ?? throw new ArgumentNullException(nameof(productType));
        _material = material ?? throw new ArgumentNullException(nameof(material));
        if (productType.Kind != Kind)
            throw new OrderValidationException($"Вид продукции «{productType.Name}» не соответствует классу {GetType().Name}.");
    }

    /// <summary>Вид продукции, который реализует конкретный класс.</summary>
    public abstract ProductKind Kind { get; }

    public ProductType ProductType
    {
        get => _productType;
        set => _productType = value ?? throw new OrderValidationException("Не выбран вид продукции.");
    }

    public Material Material
    {
        get => _material;
        set => _material = value ?? throw new OrderValidationException("Не выбран материал.");
    }

    /// <summary>Тираж — не может быть меньше 1.</summary>
    public int Quantity
    {
        get => _quantity;
        set
        {
            if (value < 1)
                throw new OrderValidationException("Тираж не может быть меньше 1.", nameof(Quantity));
            if (value > 1_000_000)
                throw new OrderValidationException("Тираж не может превышать 1 000 000 экземпляров.", nameof(Quantity));
            _quantity = value;
        }
    }

    public virtual string Format
    {
        get => _format;
        set
        {
            if (!AllowedFormats.Contains(value))
                throw new OrderValidationException($"Формат «{value}» недопустим для вида «{EnumNames.Of(Kind)}».", nameof(Format));
            _format = value;
        }
    }

    public ColorMode Color { get; set; } = ColorMode.ColorOneSide;

    public IReadOnlyList<AdditionalService> Services => _services;

    /// <summary>Допустимые форматы для данного вида продукции.</summary>
    public virtual IReadOnlyList<string> AllowedFormats => PaperFormats.ProductFormats;

    /// <summary>Коэффициент цветности печати.</summary>
    protected virtual decimal ColorFactor => Color switch
    {
        ColorMode.BlackWhite => 1.0m,
        ColorMode.ColorOneSide => 1.6m,
        ColorMode.ColorTwoSide => 2.2m,
        _ => 1m
    };

    /// <summary>Расчёт стоимости печати изделия (без доп. услуг и скидок). Реализуется в наследниках.</summary>
    public abstract decimal CalculatePrice();

    /// <summary>Сколько единиц материала (листов или м²) потребуется на тираж.</summary>
    public virtual decimal CalculateMaterialUsage()
    {
        if (Material.IsRoll)
            return Math.Round((decimal)PaperFormats.AreaM2(Format) * Quantity, 2);

        int perSheet = PaperFormats.ItemsPerSheet(Format, Material.Format);
        if (perSheet == 0)
            throw new OrderValidationException(
                $"Формат изделия {Format} больше формата листа материала {Material.Format}. Выберите другой материал.");
        return Math.Ceiling((decimal)Quantity / perSheet);
    }

    /// <summary>Стоимость материала на весь тираж.</summary>
    public decimal CalculateMaterialCost() => CalculateMaterialUsage() * Material.Price;

    /// <summary>Стоимость всех дополнительных услуг на тираж.</summary>
    public decimal CalculateServicesCost() => _services.Sum(s => s.CalculateCost(Quantity));

    public void AddService(AdditionalService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        if (_services.Any(s => s.Id == service.Id && s.Id != 0))
            throw new OrderValidationException($"Услуга «{service.Name}» уже добавлена.");
        _services.Add(service);
    }

    public void ClearServices() => _services.Clear();

    /// <summary>Краткое описание параметров изделия для таблиц и квитанции.</summary>
    public virtual string Description =>
        $"{ProductType.Name}, {Format}, {EnumNames.Of(Color)}, {Material.Name}";

    /// <summary>
    /// Дополнительные параметры конкретного вида (кол-во страниц, размеры баннера...)
    /// в виде словаря — для сохранения в БД. По умолчанию параметров нет.
    /// </summary>
    public virtual IDictionary<string, string> GetParameters() => new Dictionary<string, string>();

    /// <summary>Восстановление дополнительных параметров из словаря.</summary>
    public virtual void SetParameters(IDictionary<string, string> parameters) { }

    // --- Вспомогательные методы для сериализации параметров ---
    public string SerializeParameters() =>
        string.Join(";", GetParameters().Select(p => $"{p.Key}={p.Value}"));

    public void DeserializeParameters(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        var dict = text.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .Where(kv => kv.Length == 2)
            .ToDictionary(kv => kv[0], kv => kv[1]);
        SetParameters(dict);
    }

    protected static string Num(decimal v) => v.ToString(CultureInfo.InvariantCulture);
    protected static decimal ParseNum(string v) => decimal.Parse(v, CultureInfo.InvariantCulture);

    public override string ToString() => $"{Description} — {Quantity} {ProductType.Unit}";
}
