namespace PrintShop.Core.Models.Products;

/// <summary>
/// Визитки. Формат фиксированный 90×50 мм.
/// Цена = базовая цена за 1 шт × тираж × коэффициент цветности + материал.
/// </summary>
public class BusinessCard : PrintProduct
{
    private static readonly string[] Formats = { PaperFormats.BusinessCard };

    public BusinessCard(ProductType type, Material material) : base(type, material)
    {
        Format = PaperFormats.BusinessCard;
    }

    public override ProductKind Kind => ProductKind.BusinessCard;
    public override IReadOnlyList<string> AllowedFormats => Formats;

    public override decimal CalculatePrice() =>
        ProductType.BasePrice * Quantity * ColorFactor + CalculateMaterialCost();
}

/// <summary>
/// Листовки. Цена = базовая цена за лист A4 × коэффициент формата × тираж × цветность + материал.
/// </summary>
public class Flyer : PrintProduct
{
    private static readonly string[] Formats = { PaperFormats.A6, PaperFormats.A5, PaperFormats.A4, PaperFormats.A3 };

    public Flyer(ProductType type, Material material) : base(type, material) { }

    public override ProductKind Kind => ProductKind.Flyer;
    public override IReadOnlyList<string> AllowedFormats => Formats;

    public override decimal CalculatePrice() =>
        ProductType.BasePrice * PaperFormats.FactorToA4(Format) * Quantity * ColorFactor + CalculateMaterialCost();
}

/// <summary>
/// Буклеты (многостраничные изделия). Дополнительный параметр — количество страниц (кратно 4).
/// Цена = базовая цена за страницу A4 × страницы × коэффициент формата × тираж × цветность + материал.
/// </summary>
public class Booklet : PrintProduct
{
    private static readonly string[] Formats = { PaperFormats.A6, PaperFormats.A5, PaperFormats.A4 };
    private int _pageCount = 8;

    public Booklet(ProductType type, Material material) : base(type, material) { }

    public override ProductKind Kind => ProductKind.Booklet;
    public override IReadOnlyList<string> AllowedFormats => Formats;

    public int PageCount
    {
        get => _pageCount;
        set
        {
            if (value < 4 || value > 200 || value % 4 != 0)
                throw new OrderValidationException("Количество страниц буклета должно быть кратно 4 (от 4 до 200).", nameof(PageCount));
            _pageCount = value;
        }
    }

    public override decimal CalculatePrice() =>
        ProductType.BasePrice * PageCount * PaperFormats.FactorToA4(Format) * Quantity * ColorFactor + CalculateMaterialCost();

    /// <summary>На одном листе печатается по 2 страницы с каждой стороны.</summary>
    public override decimal CalculateMaterialUsage()
    {
        if (Material.IsRoll)
            throw new OrderValidationException("Буклеты нельзя печатать на рулонном материале.");
        int perSheet = PaperFormats.ItemsPerSheet(Format, Material.Format);
        if (perSheet == 0)
            throw new OrderValidationException($"Формат {Format} больше формата листа {Material.Format}.");
        int pagesPerSheet = perSheet * 2; // двусторонняя печать
        return Math.Ceiling((decimal)PageCount * Quantity / pagesPerSheet);
    }

    public override string Description => $"{base.Description}, {PageCount} стр.";

    public override IDictionary<string, string> GetParameters() =>
        new Dictionary<string, string> { ["Pages"] = PageCount.ToString() };

    public override void SetParameters(IDictionary<string, string> parameters)
    {
        if (parameters.TryGetValue("Pages", out var p) && int.TryParse(p, out var pages))
            PageCount = pages;
    }
}

/// <summary>
/// Плакаты (A3–A1). Цена = базовая цена за A4 × коэффициент формата × тираж × цветность + материал.
/// Для тиража до 10 шт. добавляется наценка за приладку 20%.
/// </summary>
public class Poster : PrintProduct
{
    private static readonly string[] Formats = { PaperFormats.A3, PaperFormats.A2, PaperFormats.A1 };

    public Poster(ProductType type, Material material) : base(type, material)
    {
        Format = PaperFormats.A3;
    }

    public override ProductKind Kind => ProductKind.Poster;
    public override IReadOnlyList<string> AllowedFormats => Formats;

    public override decimal CalculatePrice()
    {
        decimal print = ProductType.BasePrice * PaperFormats.FactorToA4(Format) * Quantity * ColorFactor;
        if (Quantity < 10) print *= 1.2m;
        return print + CalculateMaterialCost();
    }
}

/// <summary>
/// Баннеры — широкоформатная печать на рулонном материале.
/// Размеры задаются в метрах. Цена = базовая цена за м² × площадь × тираж + материал.
/// Баннер печатается всегда в полноцвете, поэтому коэффициент цветности = 1.
/// </summary>
public class Banner : PrintProduct
{
    private decimal _width = 1m;
    private decimal _height = 1m;

    public Banner(ProductType type, Material material) : base(type, material)
    {
        Color = ColorMode.ColorOneSide;
    }

    public override ProductKind Kind => ProductKind.Banner;
    public override IReadOnlyList<string> AllowedFormats => new[] { PaperFormats.Roll };

    /// <summary>У баннера формат задаётся размерами, поэтому свойство Format фиксировано.</summary>
    public override string Format
    {
        get => PaperFormats.Roll;
        set { /* формат баннера определяется шириной и высотой */ }
    }

    public decimal Width
    {
        get => _width;
        set => _width = CheckSize(value, nameof(Width));
    }

    public decimal Height
    {
        get => _height;
        set => _height = CheckSize(value, nameof(Height));
    }

    public decimal Area => Width * Height;

    protected override decimal ColorFactor => 1m;

    private static decimal CheckSize(decimal v, string name)
    {
        if (v <= 0 || v > 50)
            throw new OrderValidationException("Размер баннера должен быть от 0,01 до 50 м.", name);
        return v;
    }

    public override decimal CalculatePrice() =>
        ProductType.BasePrice * Area * Quantity * ColorFactor + CalculateMaterialCost();

    public override decimal CalculateMaterialUsage()
    {
        if (!Material.IsRoll)
            throw new OrderValidationException("Баннер печатается только на рулонном материале.");
        return Math.Round(Area * Quantity, 2);
    }

    public override string Description => $"{ProductType.Name}, {Width:0.##}×{Height:0.##} м, {Material.Name}";

    public override IDictionary<string, string> GetParameters() => new Dictionary<string, string>
    {
        ["W"] = Num(Width),
        ["H"] = Num(Height)
    };

    public override void SetParameters(IDictionary<string, string> parameters)
    {
        if (parameters.TryGetValue("W", out var w)) Width = ParseNum(w);
        if (parameters.TryGetValue("H", out var h)) Height = ParseNum(h);
    }
}

/// <summary>
/// Наклейки на самоклеящейся бумаге/плёнке.
/// Цена = базовая цена за A4 × коэффициент формата × тираж × цветность (+30% за фигурную вырубку) + материал.
/// </summary>
public class Sticker : PrintProduct
{
    private static readonly string[] Formats = { PaperFormats.A6, PaperFormats.A5, PaperFormats.A4 };

    public Sticker(ProductType type, Material material) : base(type, material)
    {
        Format = PaperFormats.A6;
    }

    public override ProductKind Kind => ProductKind.Sticker;
    public override IReadOnlyList<string> AllowedFormats => Formats;

    /// <summary>Фигурная (контурная) вырубка.</summary>
    public bool DieCut { get; set; }

    public override decimal CalculatePrice()
    {
        decimal print = ProductType.BasePrice * PaperFormats.FactorToA4(Format) * Quantity * ColorFactor;
        if (DieCut) print *= 1.3m;
        return print + CalculateMaterialCost();
    }

    public override string Description => DieCut ? $"{base.Description}, фигурная вырубка" : base.Description;

    public override IDictionary<string, string> GetParameters() =>
        new Dictionary<string, string> { ["DieCut"] = DieCut ? "1" : "0" };

    public override void SetParameters(IDictionary<string, string> parameters)
    {
        if (parameters.TryGetValue("DieCut", out var d)) DieCut = d == "1";
    }
}
