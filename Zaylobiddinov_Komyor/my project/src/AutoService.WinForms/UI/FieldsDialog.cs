namespace AutoService.WinForms.UI;

/// <summary>
/// Универсальный диалог ввода: поля добавляются вызовами AddText / AddNumber / AddChoice,
/// значения читаются по ключу после нажатия «ОК». Используется для простых справочных форм.
/// </summary>
public sealed class FieldsDialog : Form
{
    private readonly TableLayoutPanel _table;
    private readonly FlowLayoutPanel _buttons;
    private readonly Dictionary<string, Control> _fields = new();

    public FieldsDialog(string title)
    {
        Text = title;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        Padding = new Padding(10);

        _table = new TableLayoutPanel
        {
            ColumnCount = 2,
            AutoSize = true,
            Dock = DockStyle.Top,
        };
        _table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320));

        var ok = new Button { Text = "ОК", DialogResult = DialogResult.OK, Width = 100 };
        var cancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Width = 100 };
        _buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Bottom,
            AutoSize = true,
        };
        _buttons.Controls.AddRange([cancel, ok]);
        AcceptButton = ok;
        CancelButton = cancel;

        Controls.Add(_table);
        Controls.Add(_buttons);
    }

    /// <summary>Размер окна подбирается под количество добавленных полей.</summary>
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        var table = _table.GetPreferredSize(Size.Empty);
        var buttons = _buttons.GetPreferredSize(Size.Empty);
        ClientSize = new Size(
            Math.Max(table.Width, buttons.Width) + Padding.Horizontal,
            table.Height + buttons.Height + Padding.Vertical + 10);
    }

    public FieldsDialog AddText(string key, string label, string initial = "", bool multiline = false)
    {
        var box = new TextBox { Text = initial, Dock = DockStyle.Fill, Multiline = multiline };
        if (multiline) box.Height = 70;
        return AddRow(key, label, box);
    }

    public FieldsDialog AddNumber(string key, string label, decimal initial, decimal min, decimal max, int decimals = 0)
    {
        var box = new NumericUpDown
        {
            Minimum = min,
            Maximum = max,
            DecimalPlaces = decimals,
            Increment = decimals > 0 ? 0.1m : 1,
            ThousandsSeparator = true,
            Value = Math.Clamp(initial, min, max),
            Dock = DockStyle.Fill,
        };
        return AddRow(key, label, box);
    }

    /// <summary>Выпадающий список объектов; текст элемента задаёт функция display.</summary>
    public FieldsDialog AddChoice<T>(string key, string label, IEnumerable<T> items, Func<T, string> display)
    {
        var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
        foreach (var item in items)
            combo.Items.Add(new Choice(item!, display(item)));
        if (combo.Items.Count > 0)
            combo.SelectedIndex = 0;
        return AddRow(key, label, combo);
    }

    public string GetText(string key) => ((TextBox)_fields[key]).Text.Trim();

    public decimal GetDecimal(string key) => ((NumericUpDown)_fields[key]).Value;

    public int GetInt(string key) => (int)GetDecimal(key);

    public T GetChoice<T>(string key) =>
        ((ComboBox)_fields[key]).SelectedItem is Choice choice
            ? (T)choice.Value
            : throw new InvalidOperationException("Значение не выбрано.");

    private FieldsDialog AddRow(string key, string label, Control control)
    {
        _table.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 8, 10, 3) });
        _table.Controls.Add(control);
        _fields[key] = control;
        return this;
    }

    /// <summary>Элемент списка: объект + отображаемый текст.</summary>
    private sealed record Choice(object Value, string Text)
    {
        public override string ToString() => Text;
    }
}
