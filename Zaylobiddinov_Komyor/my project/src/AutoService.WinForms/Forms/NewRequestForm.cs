using AutoService.Domain.Entities;
using AutoService.WinForms.UI;

namespace AutoService.WinForms.Forms;

/// <summary>Приёмка автомобиля: выбор клиента и авто, пробег, описание проблемы.</summary>
public sealed class NewRequestForm : Form
{
    private readonly AppServices _app;
    private readonly ComboBox _client = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly ComboBox _car = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly ComboBox _receptionist = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly NumericUpDown _mileage = new() { Maximum = 2_000_000, ThousandsSeparator = true, Dock = DockStyle.Fill };
    private readonly TextBox _problem = new() { Multiline = true, Height = 90, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical };

    public int? CreatedRequestId { get; private set; }

    public NewRequestForm(AppServices app)
    {
        _app = app;
        Text = "Новая заявка — приёмка автомобиля";
        Width = 560;
        Height = 380;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;

        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, Padding = new Padding(10) };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        AddRow(table, "Клиент:", _client, Ui.MakeButton("Клиенты…", (_, _) => ManageClients(), 90));
        AddRow(table, "Автомобиль:", _car);
        AddRow(table, "Пробег, км:", _mileage);
        AddRow(table, "Приёмщик:", _receptionist);
        AddRow(table, "Проблема:", _problem);

        var save = Ui.MakeButton("Создать заявку", (_, _) => Save());
        var cancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Width = 100, Height = 30 };
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, AutoSize = true };
        buttons.Controls.AddRange([cancel, save]);
        CancelButton = cancel;

        Controls.Add(table);
        Controls.Add(buttons);

        _client.SelectedIndexChanged += (_, _) => FillCars();
        _car.SelectedIndexChanged += (_, _) => { if (_car.SelectedItem is Car car) _mileage.Value = car.Mileage; };
        Load += (_, _) => FillLists();
    }

    private static void AddRow(TableLayoutPanel table, string label, Control control, Control? extra = null)
    {
        table.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(3, 8, 3, 3) });
        table.Controls.Add(control);
        table.Controls.Add(extra ?? new Label());
    }

    private void FillLists()
    {
        Ui.Try(() =>
        {
            _client.Items.Clear();
            foreach (var c in _app.Clients.Search(null))
                _client.Items.Add(c);
            if (_client.Items.Count > 0) _client.SelectedIndex = 0;

            _receptionist.Items.Clear();
            _receptionist.Items.Add("— не указан —");
            foreach (var r in _app.Catalog.GetReceptionists())
                _receptionist.Items.Add(r);
            _receptionist.SelectedIndex = _receptionist.Items.Count > 1 ? 1 : 0;
        });
    }

    private void FillCars()
    {
        _car.Items.Clear();
        if (_client.SelectedItem is Client client)
            foreach (var car in client.Cars)
                _car.Items.Add(car);
        if (_car.Items.Count > 0) _car.SelectedIndex = 0;
    }

    private void ManageClients()
    {
        using (var form = new ClientsForm(_app))
            form.ShowDialog(this);
        FillLists();
    }

    private void Save()
    {
        if (_client.SelectedItem is not Client client) { Ui.Info("Выберите клиента."); return; }
        if (_car.SelectedItem is not Car car) { Ui.Info("У клиента нет автомобиля — добавьте его в справочнике клиентов."); return; }
        int? receptionistId = _receptionist.SelectedItem is Receptionist r ? r.Id : null;

        Ui.Try(() =>
        {
            var request = _app.Requests.Create(client.Id, car.Id, _problem.Text, receptionistId, (int)_mileage.Value);
            CreatedRequestId = request.Id;
            DialogResult = DialogResult.OK;
        });
    }
}
