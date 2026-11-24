using AutoService.Domain.Entities;
using AutoService.Domain.Enums;
using AutoService.WinForms.UI;

namespace AutoService.WinForms.Forms;

/// <summary>Справочники на вкладках: прайс-лист услуг, склад запчастей, сотрудники.</summary>
public sealed class CatalogForm : Form
{
    private readonly AppServices _app;
    private readonly DataGridView _services = Ui.CreateGrid();
    private readonly DataGridView _parts = Ui.CreateGrid();
    private readonly DataGridView _employees = Ui.CreateGrid();

    public CatalogForm(AppServices app)
    {
        _app = app;
        Text = "Справочники";
        Width = 950;
        Height = 560;
        StartPosition = FormStartPosition.CenterParent;

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(Tab("Услуги (прайс-лист)", _services,
            Ui.MakeButton("Новая услуга", (_, _) => AddService()),
            Ui.MakeButton("Изменить тариф", (_, _) => ChangeTariff())));
        tabs.TabPages.Add(Tab("Склад запчастей", _parts,
            Ui.MakeButton("Новая запчасть", (_, _) => AddPart()),
            Ui.MakeButton("Приход на склад", (_, _) => ReceiveParts())));
        tabs.TabPages.Add(Tab("Сотрудники", _employees,
            Ui.MakeButton("Новый механик", (_, _) => AddMechanic()),
            Ui.MakeButton("Новый приёмщик", (_, _) => AddReceptionist())));
        Controls.Add(tabs);

        Load += (_, _) => ReloadAll();
    }

    private static TabPage Tab(string title, DataGridView grid, params Control[] buttons)
    {
        var page = new TabPage(title);
        page.Controls.Add(grid);
        page.Controls.Add(Ui.ButtonBar(buttons));
        return page;
    }

    private static IEnumerable<Specialization> Specializations => Enum.GetValues<Specialization>();

    private void ReloadAll() => Ui.Try(() =>
    {
        _services.DataSource = _app.Catalog.GetServices().Select(s => new
        {
            s.Id,
            Наименование = s.Name,
            Вид_работ = s.Specialization.ToRussian(),
            Нормо_часы = s.NormHours,
            Ставка_за_час = s.HourRate,
            Цена = s.Price,
        }).ToList();

        _parts.DataSource = _app.Catalog.GetParts().Select(p => new
        {
            p.Id,
            Артикул = p.Article,
            Наименование = p.Name,
            Цена = p.Price,
            Остаток = p.Stock,
        }).ToList();

        // полиморфизм: Position у механика и приёмщика вычисляется по-разному
        _employees.DataSource = _app.Catalog.GetEmployees().Select(e => new
        {
            e.Id,
            ФИО = e.FullName,
            Должность = e.Position,
            Телефон = e.Phone,
            Принят = e.HireDate.ToString("dd.MM.yyyy"),
            Разряд = e is Mechanic m ? m.Grade.ToString() : "",
        }).ToList();
    });

    private void AddService()
    {
        using var d = new FieldsDialog("Новая услуга")
            .AddText("name", "Наименование:")
            .AddChoice("spec", "Вид работ:", Specializations, s => s.ToRussian())
            .AddNumber("hours", "Нормо-часы:", 1, 0.1m, 100, 1)
            .AddNumber("rate", "Стоимость нормо-часа:", 150, 1, 100_000, 2);
        if (d.ShowDialog(this) == DialogResult.OK
            && Ui.Try(() => _app.Catalog.AddService(d.GetText("name"), d.GetDecimal("hours"), d.GetDecimal("rate"), d.GetChoice<Specialization>("spec"))))
            ReloadAll();
    }

    private void ChangeTariff()
    {
        if (Ui.SelectedId(_services) is not int id) { Ui.Info("Выберите услугу."); return; }
        var service = _app.Catalog.GetServices().First(s => s.Id == id);

        using var d = new FieldsDialog($"Тариф: {service.Name}")
            .AddNumber("hours", "Нормо-часы:", service.NormHours, 0.1m, 100, 1)
            .AddNumber("rate", "Стоимость нормо-часа:", service.HourRate, 1, 100_000, 2);
        if (d.ShowDialog(this) == DialogResult.OK
            && Ui.Try(() => _app.Catalog.ChangeTariff(id, d.GetDecimal("hours"), d.GetDecimal("rate"))))
            ReloadAll();
    }

    private void AddPart()
    {
        using var d = new FieldsDialog("Новая запчасть")
            .AddText("article", "Артикул:")
            .AddText("name", "Наименование:")
            .AddNumber("price", "Цена:", 100, 0.01m, 1_000_000, 2)
            .AddNumber("stock", "Начальный остаток:", 0, 0, 100_000);
        if (d.ShowDialog(this) == DialogResult.OK
            && Ui.Try(() => _app.Catalog.AddPart(d.GetText("article"), d.GetText("name"), d.GetDecimal("price"), d.GetInt("stock"))))
            ReloadAll();
    }

    private void ReceiveParts()
    {
        if (Ui.SelectedId(_parts) is not int id) { Ui.Info("Выберите запчасть."); return; }

        using var d = new FieldsDialog("Приход на склад")
            .AddNumber("qty", "Количество:", 1, 1, 100_000);
        if (d.ShowDialog(this) == DialogResult.OK
            && Ui.Try(() => _app.Catalog.ReceiveParts(id, d.GetInt("qty"))))
            ReloadAll();
    }

    private void AddMechanic()
    {
        using var d = new FieldsDialog("Новый механик")
            .AddText("name", "ФИО:")
            .AddText("phone", "Телефон:", "+992 ")
            .AddChoice("spec", "Специализация:", Specializations, s => s.ToRussian())
            .AddNumber("grade", "Разряд (1–6):", 3, 1, 6);
        if (d.ShowDialog(this) == DialogResult.OK
            && Ui.Try(() => _app.Catalog.AddMechanic(d.GetText("name"), d.GetText("phone"), d.GetChoice<Specialization>("spec"), d.GetInt("grade"))))
            ReloadAll();
    }

    private void AddReceptionist()
    {
        using var d = new FieldsDialog("Новый мастер-приёмщик")
            .AddText("name", "ФИО:")
            .AddText("phone", "Телефон:", "+992 ");
        if (d.ShowDialog(this) == DialogResult.OK
            && Ui.Try(() => _app.Catalog.AddReceptionist(d.GetText("name"), d.GetText("phone"))))
            ReloadAll();
    }
}
