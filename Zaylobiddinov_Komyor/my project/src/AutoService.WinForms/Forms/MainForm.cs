using AutoService.Domain.Enums;
using AutoService.Domain.Interfaces;
using AutoService.WinForms.UI;

namespace AutoService.WinForms.Forms;

/// <summary>Главное окно: список заявок с фильтром и поиском.</summary>
public sealed class MainForm : Form
{
    private readonly AppServices _app;
    private readonly DataGridView _grid = Ui.CreateGrid();
    private readonly ComboBox _statusFilter = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
    private readonly TextBox _search = new() { Width = 220, PlaceholderText = "Номер, клиент, телефон, госномер" };
    private readonly CheckBox _hideClosed = new() { Text = "Скрыть закрытые", AutoSize = true, Checked = true };
    private readonly ToolStripStatusLabel _status = new();

    public MainForm(AppServices app)
    {
        _app = app;

        Text = "Автосервис — заявки на ремонт";
        Width = 1150;
        Height = 650;
        StartPosition = FormStartPosition.CenterScreen;

        // ---- меню ----
        var menu = new MenuStrip();
        var directories = new ToolStripMenuItem("Справочники");
        directories.DropDownItems.Add("Клиенты и автомобили", null, (_, _) => OpenDialog(new ClientsForm(_app)));
        directories.DropDownItems.Add("Услуги, склад, сотрудники", null, (_, _) => OpenDialog(new CatalogForm(_app)));
        menu.Items.Add(directories);
        menu.Items.Add(new ToolStripMenuItem("Отчёты", null, (_, _) => OpenDialog(new ReportsForm(_app))));
        MainMenuStrip = menu;

        // ---- фильтр ----
        _statusFilter.Items.Add("Все статусы");
        foreach (var s in Enum.GetValues<RequestStatus>())
            _statusFilter.Items.Add(s.ToRussian());
        _statusFilter.SelectedIndex = 0;
        _statusFilter.SelectedIndexChanged += (_, _) => LoadRequests();
        _hideClosed.CheckedChanged += (_, _) => LoadRequests();
        _search.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) LoadRequests(); };

        var filterBar = Ui.ButtonBar(
            new Label { Text = "Статус:", AutoSize = true, Margin = new Padding(3, 9, 0, 0) },
            _statusFilter,
            _search,
            Ui.MakeButton("Найти", (_, _) => LoadRequests(), 80),
            _hideClosed,
            Ui.MakeButton("Новая заявка", (_, _) => CreateRequest()),
            Ui.MakeButton("Открыть заявку", (_, _) => OpenSelected()));

        _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) OpenSelected(); };

        var statusStrip = new StatusStrip();
        statusStrip.Items.Add(_status);

        Controls.Add(_grid);
        Controls.Add(filterBar);
        Controls.Add(menu);
        Controls.Add(statusStrip);

        Load += (_, _) => LoadRequests();
    }

    private void LoadRequests()
    {
        RequestStatus? status = _statusFilter.SelectedIndex > 0
            ? (RequestStatus)(_statusFilter.SelectedIndex - 1)
            : null;
        var filter = new RequestFilter(status, _search.Text, _hideClosed.Checked);

        Ui.Try(() =>
        {
            var rows = _app.Requests.Search(filter).Select(r => new
            {
                r.Id,
                Номер = r.Number,
                Дата = r.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
                Клиент = r.Client.FullName,
                Телефон = r.Client.Phone,
                Автомобиль = r.Car.ToString(),
                Проблема = r.Problem,
                Статус = r.Status.ToRussian(),
                Сумма = r.Total,
            }).ToList();

            _grid.DataSource = rows;
            _status.Text = $"Заявок: {rows.Count}";
        });
    }

    private void CreateRequest()
    {
        using var form = new NewRequestForm(_app);
        if (form.ShowDialog(this) == DialogResult.OK && form.CreatedRequestId is int id)
        {
            LoadRequests();
            OpenRequest(id);
        }
    }

    private void OpenSelected()
    {
        if (Ui.SelectedId(_grid) is int id)
            OpenRequest(id);
        else
            Ui.Info("Выберите заявку в списке.");
    }

    private void OpenRequest(int id)
    {
        using var form = new RequestForm(_app, id);
        form.ShowDialog(this);
        LoadRequests();
    }

    private void OpenDialog(Form form)
    {
        using (form)
            form.ShowDialog(this);
        LoadRequests();
    }
}
