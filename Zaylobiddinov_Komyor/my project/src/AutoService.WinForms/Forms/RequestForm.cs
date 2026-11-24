using AutoService.Domain.Entities;
using AutoService.Domain.Enums;
using AutoService.WinForms.UI;

namespace AutoService.WinForms.Forms;

/// <summary>
/// Карточка заявки: данные клиента и авто, работы, запчасти, итог, смена статуса и оплата.
/// Кнопки смены статуса строятся по списку допустимых переходов из StatusRules.
/// </summary>
public sealed class RequestForm : Form
{
    private readonly AppServices _app;
    private readonly int _requestId;
    private RepairRequest _request = null!;

    private readonly Label _header = new() { AutoSize = true, Font = new Font("Segoe UI", 12, FontStyle.Bold), Margin = new Padding(3, 6, 3, 3) };
    private readonly Label _info = new() { AutoSize = true, Margin = new Padding(3) };
    private readonly Label _totals = new() { AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold), Margin = new Padding(3, 8, 3, 3) };
    private readonly DataGridView _works = Ui.CreateGrid();
    private readonly DataGridView _parts = Ui.CreateGrid();
    private readonly FlowLayoutPanel _statusButtons = new() { Dock = DockStyle.Bottom, AutoSize = true, Padding = new Padding(4) };
    private readonly List<Button> _editButtons = new();

    public RequestForm(AppServices app, int requestId)
    {
        _app = app;
        _requestId = requestId;

        Width = 1000;
        Height = 700;
        StartPosition = FormStartPosition.CenterParent;

        // ---- шапка ----
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, FlowDirection = FlowDirection.TopDown, AutoSize = true, Padding = new Padding(8) };
        top.Controls.AddRange([_header, _info]);

        // ---- работы ----
        var addWork = Ui.MakeButton("Добавить работу", (_, _) => AddWork());
        var removeWork = Ui.MakeButton("Удалить работу", (_, _) => RemoveWork());
        var worksBox = Section("Работы", _works, addWork, removeWork);

        // ---- запчасти ----
        var addPart = Ui.MakeButton("Добавить запчасть", (_, _) => AddPart());
        var removePart = Ui.MakeButton("Удалить запчасть", (_, _) => RemovePart());
        var partsBox = Section("Запчасти (списываются со склада сразу)", _parts, addPart, removePart);

        _editButtons.AddRange([addWork, removeWork, addPart, removePart]);

        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
        split.Panel1.Controls.Add(worksBox);
        split.Panel2.Controls.Add(partsBox);

        // ---- итог и скидка ----
        var discount = Ui.MakeButton("Скидка…", (_, _) => SetDiscount(), 100);
        var loyal = Ui.MakeButton("Скидка постоянного клиента", (_, _) => ApplyLoyal(), 200);
        var totalsBar = Ui.ButtonBar(_totals, discount, loyal);
        totalsBar.Dock = DockStyle.Bottom;

        Controls.Add(split);
        Controls.Add(top);
        Controls.Add(totalsBar);
        Controls.Add(_statusButtons);

        Load += (_, _) => Reload();
    }

    private static GroupBox Section(string title, DataGridView grid, params Control[] buttons)
    {
        var box = new GroupBox { Text = title, Dock = DockStyle.Fill, Padding = new Padding(6) };
        box.Controls.Add(grid);
        box.Controls.Add(Ui.ButtonBar(buttons));
        return box;
    }

    // ================= Отображение =================

    private void Reload()
    {
        if (!Ui.Try(() => _request = _app.Requests.Get(_requestId)))
        {
            Close();
            return;
        }

        var r = _request;
        Text = $"Заявка № {r.Number}";
        _header.Text = $"Заявка № {r.Number} — {r.Status.ToRussian()}";
        _info.Text =
            $"Клиент: {r.Client.FullName}, тел. {r.Client.Phone}\n" +
            $"Автомобиль: {r.Car}, {r.Car.Year} г., пробег {r.Car.Mileage:N0} км\n" +
            $"Создана: {r.CreatedAt:dd.MM.yyyy HH:mm}   Приёмщик: {r.Receptionist?.FullName ?? "—"}\n" +
            $"Проблема: {r.Problem}" +
            (r.Payment is null ? "" : $"\nОплата: {r.Payment.GetReceiptLine()}");

        _works.DataSource = r.Works.Select(w => new
        {
            w.Id,
            Услуга = w.Service.Name,
            Нормо_часы = w.Service.NormHours,
            Механик = w.Mechanic.FullName,
            Цена = w.Price,
        }).ToList();

        _parts.DataSource = r.Parts.Select(p => new
        {
            p.Id,
            Артикул = p.SparePart.Article,
            Запчасть = p.SparePart.Name,
            Кол_во = p.Quantity,
            Цена = p.PriceAtSale,
            Сумма = p.Sum,
        }).ToList();

        _totals.Text = $"Работы: {r.WorksSum:N2}   Запчасти: {r.PartsSum:N2}   " +
                       $"Скидка {r.DiscountPercent:0.#} %: −{r.DiscountAmount:N2}   ИТОГО: {r.Total:N2}";

        // изменять состав можно только до согласования
        foreach (var b in _editButtons)
            b.Enabled = r.IsEditable;

        BuildStatusButtons();
    }

    /// <summary>Кнопки только для допустимых переходов — пользователь не может нарушить правила.</summary>
    private void BuildStatusButtons()
    {
        _statusButtons.Controls.Clear();
        _statusButtons.Controls.Add(new Label { Text = "Действия:", AutoSize = true, Margin = new Padding(3, 9, 3, 3) });

        foreach (var next in _request.AllowedNextStatuses())
        {
            var target = next;
            var (caption, action) = target switch
            {
                RequestStatus.Issued => ("Оплатить и выдать", (Action)Pay),
                RequestStatus.Cancelled => ("Отменить заявку", (Action)Cancel),
                _ => ($"→ {target.ToRussian()}", (Action)(() => ChangeStatus(target))),
            };
            _statusButtons.Controls.Add(Ui.MakeButton(caption, (_, _) => action(), 180));
        }

        if (_request.IsClosed)
            _statusButtons.Controls.Add(new Label { Text = "Заявка закрыта.", AutoSize = true, Margin = new Padding(3, 9, 3, 3) });
    }

    // ================= Действия =================

    private void Do(Action action)
    {
        Ui.Try(action);
        Reload();
    }

    private void ChangeStatus(RequestStatus target) => Do(() => _app.Requests.ChangeStatus(_requestId, target));

    private void Cancel()
    {
        if (Ui.Confirm("Отменить заявку? Запчасти будут возвращены на склад."))
            Do(() => _app.Requests.ChangeStatus(_requestId, RequestStatus.Cancelled));
    }

    private void AddWork()
    {
        var services = _app.Catalog.GetServices();
        var mechanics = _app.Catalog.GetMechanics();
        using var dialog = new FieldsDialog("Добавить работу")
            .AddChoice("service", "Услуга:", services, s => $"{s.Name} — {s.Price:N2} ({s.Specialization.ToRussian()})")
            .AddChoice("mechanic", "Механик:", mechanics, m => $"{m.FullName} ({m.Specialization.ToRussian()}, {m.Grade} разряд)");
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var service = dialog.GetChoice<Service>("service");
        var mechanic = dialog.GetChoice<Mechanic>("mechanic");
        Do(() => _app.Requests.AddWork(_requestId, service.Id, mechanic.Id));
    }

    private void RemoveWork()
    {
        if (Ui.SelectedId(_works) is int id)
            Do(() => _app.Requests.RemoveWork(_requestId, id));
    }

    private void AddPart()
    {
        var parts = _app.Catalog.GetParts().Where(p => p.Stock > 0).ToList();
        using var dialog = new FieldsDialog("Добавить запчасть")
            .AddChoice("part", "Запчасть:", parts, p => $"{p.Name} — {p.Price:N2} (на складе {p.Stock})")
            .AddNumber("qty", "Количество:", 1, 1, 1000);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var part = dialog.GetChoice<SparePart>("part");
        var qty = dialog.GetInt("qty");
        Do(() => _app.Requests.AddPart(_requestId, part.Id, qty));
    }

    private void RemovePart()
    {
        if (Ui.SelectedId(_parts) is int id)
            Do(() => _app.Requests.RemovePart(_requestId, id));
    }

    private void SetDiscount()
    {
        using var dialog = new FieldsDialog("Скидка")
            .AddNumber("percent", "Скидка, %:", _request.DiscountPercent, 0, 50, 1);
        if (dialog.ShowDialog(this) == DialogResult.OK)
            Do(() => _app.Requests.SetDiscount(_requestId, dialog.GetDecimal("percent")));
    }

    private void ApplyLoyal() => Do(() =>
    {
        var percent = _app.Requests.ApplyLoyalDiscount(_requestId);
        Ui.Info(percent > 0
            ? $"Клиенту назначена скидка {percent} %."
            : "У клиента пока меньше 3 завершённых заявок — скидка не положена.");
    });

    private void Pay()
    {
        using var dialog = new PaymentForm(_request.Total);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        Do(() =>
        {
            var receipt = dialog.IsCard
                ? _app.Requests.PayCard(_requestId, dialog.CardLast4)
                : _app.Requests.PayCash(_requestId, dialog.Received);
            Ui.Info($"Оплата проведена, автомобиль выдан.\n\n{receipt}");
        });
    }
}
