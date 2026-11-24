namespace AutoService.WinForms.Forms;

/// <summary>Выбор способа оплаты. Проверки суммы и номера карты выполняют классы оплаты в домене.</summary>
public sealed class PaymentForm : Form
{
    private readonly decimal _total;
    private readonly RadioButton _cash = new() { Text = "Наличные", AutoSize = true, Checked = true };
    private readonly RadioButton _card = new() { Text = "Банковская карта", AutoSize = true };
    private readonly NumericUpDown _received = new() { Maximum = 10_000_000, DecimalPlaces = 2, ThousandsSeparator = true, Width = 150 };
    private readonly TextBox _last4 = new() { MaxLength = 4, Width = 80, Enabled = false };
    private readonly Label _change = new() { AutoSize = true };

    public bool IsCard => _card.Checked;
    public decimal Received => _received.Value;
    public string CardLast4 => _last4.Text;

    public PaymentForm(decimal total)
    {
        _total = total;
        Text = "Оплата заявки";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(380, 260);
        Padding = new Padding(12);

        _received.Value = Math.Min(total, _received.Maximum);
        _received.ValueChanged += (_, _) => UpdateChange();
        _cash.CheckedChanged += (_, _) =>
        {
            _received.Enabled = _cash.Checked;
            _last4.Enabled = _card.Checked;
            UpdateChange();
        };

        var table = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Dock = DockStyle.Top };
        table.Controls.Add(new Label { Text = "К оплате:", AutoSize = true });
        table.Controls.Add(new Label { Text = $"{total:N2}", AutoSize = true, Font = new Font("Segoe UI", 11, FontStyle.Bold) });
        table.Controls.Add(_cash);
        table.Controls.Add(new Label());
        table.Controls.Add(new Label { Text = "Внесено:", AutoSize = true, Margin = new Padding(20, 6, 3, 3) });
        table.Controls.Add(_received);
        table.Controls.Add(new Label { Text = "Сдача:", AutoSize = true, Margin = new Padding(20, 3, 3, 3) });
        table.Controls.Add(_change);
        table.Controls.Add(_card);
        table.Controls.Add(new Label());
        table.Controls.Add(new Label { Text = "Последние 4 цифры:", AutoSize = true, Margin = new Padding(20, 6, 3, 3) });
        table.Controls.Add(_last4);

        var ok = new Button { Text = "Провести оплату", DialogResult = DialogResult.OK, Width = 140, Height = 30 };
        var cancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Width = 100, Height = 30 };
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, AutoSize = true };
        buttons.Controls.AddRange([cancel, ok]);
        AcceptButton = ok;
        CancelButton = cancel;

        Controls.Add(table);
        Controls.Add(buttons);
        UpdateChange();
    }

    private void UpdateChange() =>
        _change.Text = _cash.Checked ? $"{Math.Max(0, _received.Value - _total):N2}" : "—";
}
