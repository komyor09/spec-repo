using AutoService.Domain.Entities;
using AutoService.WinForms.UI;

namespace AutoService.WinForms.Forms;

/// <summary>Справочник клиентов и их автомобилей.</summary>
public sealed class ClientsForm : Form
{
    private readonly AppServices _app;
    private readonly DataGridView _clients = Ui.CreateGrid();
    private readonly DataGridView _cars = Ui.CreateGrid();
    private readonly TextBox _search = new() { Width = 250, PlaceholderText = "ФИО, телефон или госномер" };
    private List<Client> _loaded = new();

    public ClientsForm(AppServices app)
    {
        _app = app;
        Text = "Клиенты и автомобили";
        Width = 1000;
        Height = 600;
        StartPosition = FormStartPosition.CenterParent;

        _search.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) LoadClients(); };
        var bar = Ui.ButtonBar(
            _search,
            Ui.MakeButton("Найти", (_, _) => LoadClients(), 80),
            Ui.MakeButton("Новый клиент", (_, _) => AddClient()),
            Ui.MakeButton("Добавить автомобиль", (_, _) => AddCar(), 160),
            Ui.MakeButton("История автомобиля", (_, _) => ShowHistory(), 160));

        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
        split.Panel1.Controls.Add(new GroupBox { Text = "Клиенты", Dock = DockStyle.Fill, Controls = { _clients } });
        split.Panel2.Controls.Add(new GroupBox { Text = "Автомобили выбранного клиента", Dock = DockStyle.Fill, Controls = { _cars } });

        Controls.Add(split);
        Controls.Add(bar);

        _clients.SelectionChanged += (_, _) => ShowCars();
        Load += (_, _) => LoadClients();
    }

    private Client? SelectedClient =>
        Ui.SelectedId(_clients) is int id ? _loaded.FirstOrDefault(c => c.Id == id) : null;

    private void LoadClients(int? selectId = null)
    {
        Ui.Try(() =>
        {
            _loaded = _app.Clients.Search(_search.Text).ToList();
            _clients.DataSource = _loaded.Select(c => new
            {
                c.Id,
                ФИО = c.FullName,
                Телефон = c.Phone,
                Email = c.Email ?? "",
                Автомобилей = c.Cars.Count,
            }).ToList();
        });

        if (selectId is int id)
            foreach (DataGridViewRow row in _clients.Rows)
                if (row.Cells["Id"].Value is int rowId && rowId == id)
                    _clients.CurrentCell = row.Cells[1];
        ShowCars();
    }

    private void ShowCars()
    {
        var client = SelectedClient;
        _cars.DataSource = (client?.Cars ?? Array.Empty<Car>()).Select(c => new
        {
            c.Id,
            Марка = c.Brand,
            Модель = c.Model,
            Год = c.Year,
            Госномер = c.PlateNumber,
            VIN = c.Vin ?? "",
            Пробег = c.Mileage,
        }).ToList();
    }

    private void AddClient()
    {
        using var dialog = new FieldsDialog("Новый клиент")
            .AddText("name", "ФИО:")
            .AddText("phone", "Телефон:", "+992 ")
            .AddText("email", "Email (необязательно):");
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        Client? created = null;
        if (Ui.Try(() => created = _app.Clients.AddClient(dialog.GetText("name"), dialog.GetText("phone"), dialog.GetText("email"))))
        {
            _search.Clear();
            LoadClients(created!.Id);
            if (Ui.Confirm("Клиент добавлен. Добавить ему автомобиль?"))
                AddCar();
        }
    }

    private void AddCar()
    {
        var client = SelectedClient;
        if (client is null) { Ui.Info("Выберите клиента."); return; }

        using var dialog = new FieldsDialog($"Автомобиль клиента {client.FullName}")
            .AddText("brand", "Марка:")
            .AddText("model", "Модель:")
            .AddNumber("year", "Год выпуска:", DateTime.Today.Year - 5, 1950, DateTime.Today.Year + 1)
            .AddText("plate", "Госномер:")
            .AddText("vin", "VIN (необязательно):")
            .AddNumber("mileage", "Пробег, км:", 0, 0, 2_000_000);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        if (Ui.Try(() => _app.Clients.AddCar(client.Id, dialog.GetText("brand"), dialog.GetText("model"),
                dialog.GetInt("year"), dialog.GetText("plate"), dialog.GetText("vin"), dialog.GetInt("mileage"))))
            LoadClients(client.Id);
    }

    private void ShowHistory()
    {
        if (Ui.SelectedId(_cars) is not int carId) { Ui.Info("Выберите автомобиль."); return; }

        Ui.Try(() =>
        {
            var rows = _app.Reports.CarHistory(carId).Select(h => new
            {
                Номер = h.Number,
                Дата = h.CreatedAt.ToString("dd.MM.yyyy"),
                Статус = h.Status,
                Проблема = h.Problem,
                Работы = h.Works,
                Сумма = h.Total,
            }).ToList();
            Ui.ShowList(this, "История обслуживания автомобиля", rows);
        });
    }
}
