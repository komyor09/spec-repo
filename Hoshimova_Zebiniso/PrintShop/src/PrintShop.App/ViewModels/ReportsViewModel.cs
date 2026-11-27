using System.Collections.ObjectModel;
using PrintShop.App.Infrastructure;
using PrintShop.Core.Models;
using PrintShop.Core.Services;

namespace PrintShop.App.ViewModels;

/// <summary>Раздел «Отчёты»: выручка, заказы по видам продукции, остатки материалов, экспорт в CSV.</summary>
public class ReportsViewModel : SectionViewModel
{
    private readonly AppServices _app;
    private DateTime _from = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime _to = DateTime.Today;
    private int _tabIndex;
    private bool _onlyLowMaterials;

    public ReportsViewModel(AppServices app)
    {
        _app = app;
        BuildCommand = new RelayCommand(Refresh);
        ExportCommand = new RelayCommand(Export);
        PeriodCommand = new RelayCommand(p => SetPeriod(p as string));
    }

    public override string Title => "Отчёты";

    public DateTime From { get => _from; set => Set(ref _from, value); }
    public DateTime To { get => _to; set => Set(ref _to, value); }
    public int TabIndex { get => _tabIndex; set => Set(ref _tabIndex, value); }
    public bool OnlyLowMaterials { get => _onlyLowMaterials; set { if (Set(ref _onlyLowMaterials, value)) Refresh(); } }

    public ObservableCollection<RevenueRow> Revenue { get; } = new();
    public ObservableCollection<ProductReportRow> ByProducts { get; } = new();
    public ObservableCollection<MaterialReportRow> Materials { get; } = new();

    public string RevenueTotal { get; private set; } = "";
    public string ProductsTotal { get; private set; } = "";
    public string MaterialsTotal { get; private set; } = "";

    // Сводка
    public int ActiveOrders { get; private set; }
    public int OverdueOrders { get; private set; }
    public int DueTodayOrders { get; private set; }
    public decimal TotalDebt { get; private set; }

    public RelayCommand BuildCommand { get; }
    public RelayCommand ExportCommand { get; }
    public RelayCommand PeriodCommand { get; }

    private void SetPeriod(string? period)
    {
        var today = DateTime.Today;
        (From, To) = period switch
        {
            "today" => (today, today),
            "week" => (today.AddDays(-6), today),
            "month" => (new DateTime(today.Year, today.Month, 1), today),
            "year" => (new DateTime(today.Year, 1, 1), today),
            _ => (From, To)
        };
        Refresh();
    }

    public override void Refresh()
    {
        if (From > To) throw new OrderValidationException("Начальная дата периода больше конечной.");

        Revenue.Clear();
        foreach (var r in _app.Reports.GetRevenue(From, To)) Revenue.Add(r);
        RevenueTotal = $"Итого за период: {Revenue.Sum(r => r.Total):N2} сом  (оплат: {Revenue.Sum(r => r.PaymentsCount)})";

        ByProducts.Clear();
        foreach (var r in _app.Reports.GetByProducts(From, To)) ByProducts.Add(r);
        ProductsTotal = $"Всего позиций: {ByProducts.Sum(r => r.ItemsCount)}, на сумму {ByProducts.Sum(r => r.Revenue):N2} сом";

        Materials.Clear();
        foreach (var r in _app.Reports.GetMaterials(OnlyLowMaterials)) Materials.Add(r);
        MaterialsTotal = $"Позиций: {Materials.Count}, ниже минимума: {Materials.Count(m => m.BelowMinimum)}, " +
                         $"стоимость остатков: {Materials.Sum(m => m.StockValue):N2} сом";

        (ActiveOrders, OverdueOrders, DueTodayOrders, TotalDebt) = _app.Reports.GetSummary(DateTime.Today);

        foreach (var name in new[] { nameof(RevenueTotal), nameof(ProductsTotal), nameof(MaterialsTotal),
                     nameof(ActiveOrders), nameof(OverdueOrders), nameof(DueTodayOrders), nameof(TotalDebt) })
            OnPropertyChanged(name);
    }

    private void Export()
    {
        string period = $"{From:dd.MM.yyyy} – {To:dd.MM.yyyy}";
        switch (TabIndex)
        {
            case 0:
                ExportTo("revenue", $"Отчёт о выручке за {period}", Revenue, new[]
                {
                    new CsvColumn<RevenueRow>("Дата", r => r.Date),
                    new CsvColumn<RevenueRow>("Кол-во оплат", r => r.PaymentsCount),
                    new CsvColumn<RevenueRow>("Предоплаты", r => r.Prepayments),
                    new CsvColumn<RevenueRow>("Окончательные оплаты", r => r.FinalPayments),
                    new CsvColumn<RevenueRow>("Итого", r => r.Total),
                });
                break;
            case 1:
                ExportTo("products", $"Заказы по видам продукции за {period}", ByProducts, new[]
                {
                    new CsvColumn<ProductReportRow>("Вид продукции", r => r.ProductName),
                    new CsvColumn<ProductReportRow>("Заказов", r => r.OrdersCount),
                    new CsvColumn<ProductReportRow>("Позиций", r => r.ItemsCount),
                    new CsvColumn<ProductReportRow>("Общий тираж", r => r.TotalQuantity),
                    new CsvColumn<ProductReportRow>("Сумма", r => r.Revenue),
                });
                break;
            default:
                ExportTo("materials", $"Остатки материалов на {DateTime.Now:dd.MM.yyyy HH:mm}", Materials, new[]
                {
                    new CsvColumn<MaterialReportRow>("Материал", r => r.Name),
                    new CsvColumn<MaterialReportRow>("Плотность", r => r.Density),
                    new CsvColumn<MaterialReportRow>("Формат", r => r.Format),
                    new CsvColumn<MaterialReportRow>("Остаток", r => r.Quantity),
                    new CsvColumn<MaterialReportRow>("Мин. запас", r => r.MinQuantity),
                    new CsvColumn<MaterialReportRow>("Ед.", r => r.Unit),
                    new CsvColumn<MaterialReportRow>("Цена", r => r.Price),
                    new CsvColumn<MaterialReportRow>("Стоимость остатка", r => r.StockValue),
                    new CsvColumn<MaterialReportRow>("Состояние", r => r.State),
                });
                break;
        }
    }

    private static void ExportTo<T>(string name, string title, IEnumerable<T> rows, IReadOnlyList<CsvColumn<T>> columns)
    {
        var path = DialogProvider.Service.SaveFile("Файл CSV (*.csv)|*.csv", $"{name}_{DateTime.Now:yyyyMMdd}.csv");
        if (path == null) return;
        ReportService.ExportCsv(path, rows, columns, title);
        Ui.Info($"Отчёт сохранён в файл:\n{path}");
    }
}
