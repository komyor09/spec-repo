namespace PrintShop.Core.Models;

/// <summary>Справочник форматов бумаги и их размеров в миллиметрах.</summary>
public static class PaperFormats
{
    public const string BusinessCard = "90x50";
    public const string A6 = "A6";
    public const string A5 = "A5";
    public const string A4 = "A4";
    public const string A3 = "A3";
    public const string SRA3 = "SRA3";
    public const string A2 = "A2";
    public const string A1 = "A1";
    public const string Roll = "Рулон";

    private static readonly Dictionary<string, (int W, int H)> Sizes = new()
    {
        [BusinessCard] = (90, 50),
        [A6] = (105, 148),
        [A5] = (148, 210),
        [A4] = (210, 297),
        [A3] = (297, 420),
        [SRA3] = (320, 450),
        [A2] = (420, 594),
        [A1] = (594, 841),
    };

    /// <summary>Форматы для печатной продукции (без рулона).</summary>
    public static IReadOnlyList<string> ProductFormats { get; } = new[] { A6, A5, A4, A3, A2, A1 };

    /// <summary>Форматы для материалов.</summary>
    public static IReadOnlyList<string> MaterialFormats { get; } = new[] { A4, A3, SRA3, A2, A1, Roll };

    public static bool IsKnown(string format) => format == Roll || Sizes.ContainsKey(format);

    public static (int W, int H) SizeOf(string format) =>
        Sizes.TryGetValue(format, out var s)
            ? s
            : throw new OrderValidationException($"Неизвестный формат «{format}».");

    /// <summary>Площадь формата в м².</summary>
    public static double AreaM2(string format)
    {
        var (w, h) = SizeOf(format);
        return w * h / 1_000_000.0;
    }

    /// <summary>Коэффициент формата относительно A4 (A4 = 1, A3 = 2, A5 = 0.5 ...).</summary>
    public static decimal FactorToA4(string format) => (decimal)(AreaM2(format) / AreaM2(A4));

    /// <summary>
    /// Сколько изделий формата <paramref name="product"/> помещается на листе формата <paramref name="sheet"/>
    /// (раскладка с учётом поворота, без учёта вылетов).
    /// </summary>
    public static int ItemsPerSheet(string product, string sheet)
    {
        var (pw, ph) = SizeOf(product);
        var (sw, sh) = SizeOf(sheet);
        int straight = (sw / pw) * (sh / ph);
        int rotated = (sw / ph) * (sh / pw);
        return Math.Max(straight, rotated);
    }
}
