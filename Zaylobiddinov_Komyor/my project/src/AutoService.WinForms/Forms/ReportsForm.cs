using AutoService.WinForms.UI;

namespace AutoService.WinForms.Forms;

/// <summary>Отчёты руководителя: выручка и загрузка механиков за период.</summary>
public sealed class ReportsForm : Form
{
    private readonly AppServices _app;
    private readonly DateTimePicker _from = new() { Format = DateTimePickerFormat.Short, Width = 120 };
    private readonly DateTimePicker _to = new() { Format = DateTimePickerFormat.Short, Width = 120 };
    private readonly DataGridView _grid = Ui.CreateGrid();
    private readonly Label _summary = new() { Dock = DockStyle.Bottom, Height = 30, Padding = new Padding(6), Font = new Font("Segoe UI", 10, FontStyle.Bold) };

    public ReportsForm(AppServices app)
    {
        _app = app;
        Text = "Отчёты";
        Width = 950;
        Height = 550;
        StartPosition = FormStartPosition.CenterParent;

        var today = DateTime.Today;
        _from.Value = new DateTime(today.Year, today.Month, 1);
        _to.Value = today;

        var bar = Ui.ButtonBar(
            new Label { Text = "Период с", AutoSize = true, Margin = new Padding(3, 9, 0, 0) }, _from,
            new Label { Text = "по", AutoSize = true, Margin = new Padding(3, 9, 0, 0) }, _to,
            Ui.MakeButton("Выручка", (_, _) => ShowRevenue(), 120),
            Ui.MakeButton("Загрузка механиков", (_, _) => ShowMechanicLoad(), 160));

        Controls.Add(_grid);
        Controls.Add(bar);
        Controls.Add(_summary);
    }

    private void ShowRevenue() => Ui.Try(() =>
    {
        var report = _app.Reports.Revenue(_from.Value, _to.Value);
        _grid.DataSource = report.Rows.Select(r => new
        {
            Номер = r.Number,
            Выдана = r.IssuedAt.ToString("dd.MM.yyyy HH:mm"),
            Клиент = r.Client,
            Автомобиль = r.Car,
            Оплата = r.PaymentMethod,
            Сумма = r.Total,
        }).ToList();
        _summary.Text = $"Выдано заявок: {report.Count}   Выручка: {report.Total:N2}   Средний чек: {report.AverageCheck:N2}";
    });

    private void ShowMechanicLoad() => Ui.Try(() =>
    {
        var rows = _app.Reports.MechanicLoad(_from.Value, _to.Value);
        _grid.DataSource = rows.Select(r => new
        {
            Механик = r.Mechanic,
            Специализация = r.Specialization,
            Выполнено_работ = r.WorksCount,
            На_сумму = r.WorksSum,
        }).ToList();
        _summary.Text = $"Механиков: {rows.Count}   Всего работ: {rows.Sum(r => r.WorksCount)}";
    });
}
