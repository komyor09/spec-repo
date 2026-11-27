using System.Globalization;
using System.Text;
using PrintShop.Core.Data;
using PrintShop.Core.Models;

namespace PrintShop.Core.Services;

/// <summary>Строка отчёта о выручке.</summary>
public record RevenueRow(DateTime Date, int PaymentsCount, decimal Prepayments, decimal FinalPayments, decimal Total);

/// <summary>Строка отчёта по видам продукции.</summary>
public record ProductReportRow(string ProductName, int OrdersCount, int ItemsCount, long TotalQuantity, decimal Revenue);

/// <summary>Строка отчёта по материалам.</summary>
public record MaterialReportRow(string Name, int Density, string Format, decimal Quantity, decimal MinQuantity,
    string Unit, decimal Price, decimal StockValue, bool BelowMinimum)
{
    public string State => BelowMinimum ? "Ниже минимума" : "Норма";
}

/// <summary>Колонка для экспорта в CSV.</summary>
public record CsvColumn<T>(string Header, Func<T, object?> Value);

/// <summary>Построение отчётов (LINQ) и экспорт в CSV.</summary>
public class ReportService
{
    private readonly OrderRepository _orders;
    private readonly MaterialRepository _materials;

    public ReportService(OrderRepository orders, MaterialRepository materials)
    {
        _orders = orders;
        _materials = materials;
    }

    /// <summary>Выручка за период — по фактически полученным оплатам, сгруппированным по дням.</summary>
    public IReadOnlyList<RevenueRow> GetRevenue(DateTime from, DateTime to) =>
        _orders.GetAll()
            .SelectMany(o => o.Payments)
            .Where(p => p.PaymentDate.Date >= from.Date && p.PaymentDate.Date <= to.Date)
            .GroupBy(p => p.PaymentDate.Date)
            .OrderBy(g => g.Key)
            .Select(g => new RevenueRow(
                g.Key,
                g.Count(),
                g.Where(p => p.PaymentType == PaymentType.Prepayment).Sum(p => p.Amount),
                g.Where(p => p.PaymentType == PaymentType.Final).Sum(p => p.Amount),
                g.Sum(p => p.Amount)))
            .ToList();

    /// <summary>Количество заказов и сумма по видам продукции за период (без отменённых).</summary>
    public IReadOnlyList<ProductReportRow> GetByProducts(DateTime from, DateTime to) =>
        _orders.GetAll()
            .Where(o => o.Status != OrderStatus.Cancelled
                        && o.CreatedAt.Date >= from.Date && o.CreatedAt.Date <= to.Date)
            .SelectMany(o => o.Items, (o, item) => new { Order = o, Item = item })
            .GroupBy(x => x.Item.Product.ProductType.Name)
            .Select(g => new ProductReportRow(
                g.Key,
                g.Select(x => x.Order.Id).Distinct().Count(),
                g.Count(),
                g.Sum(x => (long)x.Item.Quantity),
                g.Sum(x => x.Item.Price)))
            .OrderByDescending(r => r.Revenue)
            .ToList();

    /// <summary>Остатки материалов; при onlyLow = true — только ниже минимального запаса.</summary>
    public IReadOnlyList<MaterialReportRow> GetMaterials(bool onlyLow = false) =>
        _materials.GetAll()
            .Where(m => !onlyLow || m.IsBelowMinimum)
            .OrderByDescending(m => m.IsBelowMinimum).ThenBy(m => m.Name)
            .Select(m => new MaterialReportRow(m.Name, m.Density, m.Format, m.Quantity, m.MinQuantity, m.Unit,
                m.Price, Math.Round(m.Quantity * m.Price, 2), m.IsBelowMinimum))
            .ToList();

    /// <summary>Сводные показатели для главного экрана отчётов.</summary>
    public (int Active, int Overdue, int DueToday, decimal TotalDebt) GetSummary(DateTime today)
    {
        var orders = _orders.GetAll();
        return (
            orders.Count(o => !o.IsClosed),
            orders.Count(o => o.GetDueState(today) == DueState.Overdue),
            orders.Count(o => o.GetDueState(today) == DueState.Today),
            orders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.Debt));
    }

    /// <summary>
    /// Обобщённый экспорт любой коллекции в CSV (разделитель «;», кодировка UTF-8 с BOM — корректно открывается в Excel).
    /// </summary>
    public static void ExportCsv<T>(string path, IEnumerable<T> rows, IReadOnlyList<CsvColumn<T>> columns, string? title = null)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(title)) sb.AppendLine(Escape(title));
        sb.AppendLine(string.Join(";", columns.Select(c => Escape(c.Header))));
        foreach (var row in rows)
            sb.AppendLine(string.Join(";", columns.Select(c => Escape(Format(c.Value(row))))));
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    private static string Format(object? v) => v switch
    {
        null => string.Empty,
        DateTime d => d.ToString(d.TimeOfDay == TimeSpan.Zero ? "dd.MM.yyyy" : "dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("ru-RU")),
        decimal m => m.ToString("0.00", CultureInfo.GetCultureInfo("ru-RU")),
        bool b => b ? "Да" : "Нет",
        _ => Convert.ToString(v, CultureInfo.GetCultureInfo("ru-RU")) ?? string.Empty
    };

    private static string Escape(string s) =>
        s.IndexOfAny(new[] { ';', '"', '\n', '\r' }) >= 0 ? $"\"{s.Replace("\"", "\"\"")}\"" : s;
}
