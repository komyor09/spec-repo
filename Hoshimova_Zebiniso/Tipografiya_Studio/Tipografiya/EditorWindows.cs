using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace Tipografiya;

internal sealed class EntryWindow : Window
{
    public StackPanel Fields { get; } = new();
    private readonly WrapPanel commands = new() { HorizontalAlignment = HorizontalAlignment.Right };
    private readonly TextBlock error = new() { Foreground = Brushes.Firebrick, TextWrapping = TextWrapping.Wrap };
    private Func<bool>? save;
    private Func<string>? state;
    private string initial = "";
    private bool accepted;

    public EntryWindow(Window owner, string title, int width = 510, int height = 450)
    {
        Owner = owner;
        Title = title;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Width = Math.Min(width, SystemParameters.WorkArea.Width);
        Height = Math.Min(height, SystemParameters.WorkArea.Height);
        MinWidth = Math.Min(380, Width);
        MinHeight = Math.Min(320, Height);
        Background = (Brush)FindResource("Paper");
        FontFamily = new FontFamily("Segoe UI");
        FontSize = 14;
        Foreground = (Brush)FindResource("Ink");
        Language = XmlLanguage.GetLanguage("ru-RU");

        var layout = new DockPanel { Margin = new Thickness(16) };
        Content = layout;
        var heading = new TextBlock { Text = title, Style = (Style)FindResource("Title"),
            Margin = new Thickness(0, 0, 0, 12), TextWrapping = TextWrapping.Wrap };
        DockPanel.SetDock(heading, Dock.Top);
        layout.Children.Add(heading);
        var footer = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
        footer.Children.Add(error);
        footer.Children.Add(commands);
        DockPanel.SetDock(footer, Dock.Bottom);
        layout.Children.Add(footer);
        var card = new Border { Style = (Style)FindResource("Card"),
            Padding = new Thickness(14, 6, 14, 14), Child = Fields };
        layout.Children.Add(new ScrollViewer { Content = card, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
    }

    public static Button ButtonFor(string text, Action action, bool primary = false)
    {
        var b = new Button { Content = text, Margin = new Thickness(0, 5, 6, 0) };
        if (primary) b.SetResourceReference(StyleProperty, "Primary");
        b.Click += (_, _) => action();
        return b;
    }

    public static StackPanel Field(string label, Control input)
    {
        AutomationProperties.SetName(input, label);
        var area = new StackPanel { Margin = new Thickness(0, 5, 0, 4) };
        area.Children.Add(new Label { Content = label, Target = input, FontSize = 12,
            FontWeight = FontWeights.SemiBold, Padding = new Thickness(0, 0, 0, 4) });
        area.Children.Add(input);
        return area;
    }

    public void Pair(FrameworkElement left, FrameworkElement right)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition());
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
        row.ColumnDefinitions.Add(new ColumnDefinition());
        Grid.SetColumn(right, 2);
        row.Children.Add(left);
        row.Children.Add(right);
        Fields.Children.Add(row);
    }

    public static Border Highlight(UIElement content) => new()
    {
        Child = content, Background = new SolidColorBrush(Color.FromRgb(231, 239, 222)),
        CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 9, 12, 9),
        Margin = new Thickness(0, 8, 0, 0)
    };

    public bool Error(string message, Control? field = null)
    {
        error.Text = message;
        field?.Focus();
        return false;
    }

    public bool Run(string verb = "Сохранить", Func<bool>? apply = null, Func<string>? snapshot = null)
    {
        save = apply;
        state = snapshot;
        initial = state?.Invoke() ?? "";
        commands.Children.Add(ButtonFor(apply == null ? "Закрыть" : "Назад", Close));
        if (apply != null)
            commands.Children.Add(ButtonFor(verb, () =>
            {
                if (!apply()) return;
                accepted = true;
                DialogResult = true;
            }, true));

        ShowDialog();
        return accepted;
    }

    public bool RunConfirmation(string verb)
    {
        var cancel = ButtonFor("Отмена", () => DialogResult = false);
        cancel.IsCancel = true;
        cancel.IsDefault = true;
        commands.Children.Add(cancel);
        var confirm = ButtonFor(verb, () => { accepted = true; DialogResult = true; });
        confirm.SetResourceReference(StyleProperty, "Danger");
        confirm.Margin = new Thickness(0, 5, 0, 0);
        commands.Children.Add(confirm);
        Loaded += (_, _) => cancel.Focus();
        return ShowDialog() == true;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!accepted && save != null && state != null && state() != initial)
        {
            var result = MessageBox.Show(this, "Сохранить изменения перед закрытием?",
                Title, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                accepted = save();
                e.Cancel = !accepted;
            }
            else if (result == MessageBoxResult.Cancel) e.Cancel = true;
        }
        base.OnClosing(e);
    }
}

internal static class EditorWindows
{
    public static bool ConfirmDelete(Window owner, string title, string item, string consequence)
    {
        var w = new EntryWindow(owner, title, 540, 355);
        w.Fields.Children.Add(new TextBlock
        {
            Text = item, FontSize = 17, FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 12)
        });
        w.Fields.Children.Add(new TextBlock { Text = consequence, TextWrapping = TextWrapping.Wrap });
        w.Fields.Children.Add(new TextBlock
        {
            Text = "Восстановить удалённые данные нельзя.", TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.FromRgb(148, 70, 56)), Margin = new Thickness(0, 12, 0, 0)
        });
        return w.RunConfirmation("Удалить");
    }

    public static void DeletionUnavailable(Window owner, string message)
    {
        var w = new EntryWindow(owner, "Удаление недоступно", 540, 320);
        w.Fields.Children.Add(new TextBlock
        {
            Text = message, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0)
        });
        w.Run();
    }

    private static ComboBox Choice(params string[] values) => new() { ItemsSource = values, SelectedIndex = 0 };

    public static Client? EditClient(Window owner, int id, Client? existing = null)
    {
        var w = new EntryWindow(owner, existing == null ? "Новый клиент" : "Карточка клиента", 510, 325);
        var name = new TextBox { Text = existing?.Name ?? "" };
        var phone = new TextBox { Text = existing?.Phone ?? "" };
        w.Fields.Children.Add(EntryWindow.Field("Имя или организация", name));
        w.Fields.Children.Add(EntryWindow.Field("Контактный телефон", phone));
        Client? result = null;
        bool Save()
        {
            if (string.IsNullOrWhiteSpace(name.Text)) return w.Error("Введите имя или организацию.", name);
            if (Display.Digits(phone.Text).Length == 0) return w.Error("Введите телефон с цифрами.", phone);
            result = existing ?? new Client { Id = id };
            result.Name = name.Text.Trim();
            result.Phone = phone.Text.Trim();
            return true;
        }
        return w.Run("Сохранить клиента", Save, () => $"{name.Text}\n{phone.Text}") ? result : null;
    }

    public static bool EditTariff(Window owner, TariffCatalog catalog, Tariff? old = null)
    {
        var w = new EntryWindow(owner, old == null ? "Новый тариф" : "Изменить тариф", 550, 410);
        var name = new TextBox { Text = old?.Name ?? "", MaxLength = 140 };
        var unitValues = Enum.GetValues<PriceUnit>();
        var unit = Choice(unitValues.Select(Tariff.Label).ToArray());
        unit.SelectedIndex = Array.IndexOf(unitValues, old?.Unit ?? PriceUnit.Piece);
        unit.IsEnabled = old?.IsBinding != true;
        var rate = new TextBox { Text = old?.Rate.ToString("F2", Display.Ru) ?? "" };
        w.Fields.Children.Add(EntryWindow.Field("Название услуги или варианта печати", name));
        w.Pair(EntryWindow.Field("Единица расчёта", unit), EntryWindow.Field("Цена за единицу, сомони", rate));
        var id = old?.Id ?? Guid.NewGuid().ToString("N");
        bool Save()
        {
            if (string.IsNullOrWhiteSpace(name.Text)) return w.Error("Введите название тарифа.", name);
            if (!TariffCatalog.TryNumber(rate.Text, 2, out decimal price))
                return w.Error("Введите цену с точностью до двух знаков, например 3,50.", rate);
            var result = new Tariff(id, name.Text.Trim(), unitValues[unit.SelectedIndex], price);
            string? issue = catalog.Validate(result);
            if (issue != null) return w.Error(issue);
            try { catalog.Save(result); }
            catch (Exception ex) when (Database.IsStorageError(ex))
            {
                return w.Error($"Не удалось сохранить прайс. {ex.Message}");
            }
            return true;
        }
        return w.Run("Сохранить тариф", Save, () => $"{name.Text}|{unit.SelectedIndex}|{rate.Text}");
    }

    public static OrderLine? EditLine(Window owner, TariffCatalog prices, OrderLine? old = null)
    {
        var w = new EntryWindow(owner, old == null ? "Новая позиция" : "Позиция заказа", 610, 520);
        // Keep the agreed tariff when editing an existing line, even after a price/unit change.
        var choices = prices.Items.Select(t => t.Id == old?.Tariff.Id ? old!.Tariff : t).ToList();
        if (old != null && choices.All(t => t.Id != old.Tariff.Id))
            choices.Insert(0, old.Tariff);
        var tariff = new ComboBox { ItemsSource = choices, DisplayMemberPath = "Name",
            IsTextSearchEnabled = true, MaxDropDownHeight = 330 };
        tariff.SelectedItem = old?.Tariff ?? choices.First();
        var product = Choice("Листовка", "Брошюра", "Печать сторон документа");
        product.SelectedIndex = old?.Product switch
        {
            PrintProduct.Brochure => 1, PrintProduct.Service => 2, _ => 0
        };
        var quantity = new TextBox { Text = old?.QuantityText ?? "1" };
        var units = new TextBox { Text = old?.Units.ToString() ?? "1" };
        w.Fields.Children.Add(EntryWindow.Field("Тариф", tariff));
        var productField = EntryWindow.Field("Изделие", product);
        w.Fields.Children.Add(productField);
        var qtyField = EntryWindow.Field("Количество", quantity);
        var unitsField = EntryWindow.Field("Печатных сторон", units);
        w.Pair(qtyField, unitsField);
        var detail = new TextBlock { TextWrapping = TextWrapping.Wrap };
        var cost = new TextBlock { FontFamily = new FontFamily("Georgia"), FontSize = 24 };
        var panel = new StackPanel();
        panel.Children.Add(detail);
        panel.Children.Add(cost);
        w.Fields.Children.Add(EntryWindow.Highlight(panel));

        Tariff Active() => (Tariff)tariff.SelectedItem;
        PrintProduct Product() => Active().Unit != PriceUnit.PrintSide ? PrintProduct.Service :
            product.SelectedIndex switch { 0 => PrintProduct.Flyer, 1 => PrintProduct.Brochure, _ => PrintProduct.Service };
        OrderLine? Read(out string problem)
        {
            var selected = Active();
            var kind = Product();
            problem = "";
            if (!TariffCatalog.TryNumber(quantity.Text, selected.Measured ? 3 : 0, out decimal qty)
                || qty <= 0 || qty > int.MaxValue)
            {
                problem = selected.Measured ? "Введите положительный объём до 2 147 483 647 с точностью до 0,001."
                                            : "Укажите положительное целое количество до 2 147 483 647.";
                return null;
            }
            int n = 1;
            if (kind != PrintProduct.Service && (!int.TryParse(units.Text, out n)
                || (kind == PrintProduct.Brochure ? n < 4 || n % 4 != 0 : n != 1 && n != 2)))
            {
                problem = kind == PrintProduct.Brochure ? "Число страниц должно делиться на 4: 4, 8, 12…"
                                                        : "Листовка может иметь одну или две стороны.";
                return null;
            }
            bool keep = old != null && old.Tariff.Id == selected.Id && old.Product == kind;
            var applied = keep ? old!.Tariff : prices.Items.FirstOrDefault(t => t.Id == selected.Id);
            if (applied == null)
            {
                problem = "Этот тариф удалён. Для другого вида изделия выберите действующий тариф.";
                return null;
            }
            if (applied.Unit != selected.Unit)
            {
                problem = "Единица действующего тарифа изменилась. Для нового варианта добавьте отдельную позицию.";
                return null;
            }
            decimal binding = kind == PrintProduct.Brochure ? (keep ? old!.Binding : prices.BindingRate) : 0m;
            return new OrderLine(applied, qty, kind, n, binding);
        }

        void LabelField(StackPanel field, Control input, string label)
        {
            ((Label)field.Children[0]).Content = label;
            AutomationProperties.SetName(input, label);
        }
        void Update()
        {
            var t = Active();
            var kind = Product();
            productField.Visibility = t.Unit == PriceUnit.PrintSide ? Visibility.Visible : Visibility.Collapsed;
            unitsField.Visibility = kind == PrintProduct.Service ? Visibility.Collapsed : Visibility.Visible;
            LabelField(qtyField, quantity, kind != PrintProduct.Service ? "Тираж, экземпляров" :
                $"Количество, {(t.Unit == PriceUnit.PrintSide ? "печатных сторон" : t.UnitText)}");
            LabelField(unitsField, units, kind == PrintProduct.Brochure ? "Страниц в брошюре" : "Сторон на листовке");
            var line = Read(out string problem);
            detail.Text = line == null ? problem : $"{line.Formula}   ·   ставка {line.Tariff.Price} сомони/{line.Tariff.UnitText}";
            detail.Foreground = line == null ? Brushes.Firebrick : Brushes.DarkSlateGray;
            cost.Text = line == null ? "" : $"{line.AmountText} сомони";
        }
        tariff.SelectionChanged += (_, _) => Update();
        product.SelectionChanged += (_, _) => { units.Text = Product() == PrintProduct.Brochure ? "4" : "1"; Update(); };
        quantity.TextChanged += (_, _) => Update();
        units.TextChanged += (_, _) => Update();
        Update();

        OrderLine? result = null;
        bool Save()
        {
            result = Read(out string message);
            return result != null || w.Error(message);
        }
        return w.Run("Применить", Save, () =>
            $"{Active().Id}|{product.SelectedIndex}|{quantity.Text}|{units.Text}") ? result : null;
    }

    public static PrintOrder? EditOrder(Window owner, List<Client> clients, TariffCatalog prices, int nextId, PrintOrder? old = null)
    {
        bool editable = old == null || old.Status == "Принят";
        var w = new EntryWindow(owner, old == null ? "Новый заказ" : $"Заказ № {old.Id}", 740, 690);
        var customer = new ComboBox { IsEnabled = editable };
        void Reload(Client? value)
        {
            customer.ItemsSource = clients.OrderBy(c => c.Name).ToList();
            customer.SelectedItem = value;
        }
        Reload(old?.Customer);
        var customerField = EntryWindow.Field("Заказчик", customer);
        if (editable)
        {
            var newClient = EntryWindow.ButtonFor("+ Новый клиент", () =>
            {
                var client = EditClient(w, clients.Select(c => c.Id).DefaultIfEmpty().Max() + 1);
                if (client == null) return;
                clients.Add(client);
                Reload(client);
            });
            newClient.HorizontalAlignment = HorizontalAlignment.Left;
            customerField.Children.Add(newClient);
        }
        var due = new DatePicker { SelectedDate = old?.Due ?? DateTime.Today.AddDays(2), IsEnabled = editable };
        w.Pair(customerField, EntryWindow.Field("Срок готовности", due));
        var comment = new TextBox { Text = old?.Comment ?? "", IsReadOnly = !editable, TextWrapping = TextWrapping.Wrap };
        w.Fields.Children.Add(EntryWindow.Field("Комментарий", comment));

        var lines = old?.Lines.ToList() ?? new List<OrderLine>();
        var grid = new DataGrid { MinHeight = 105, MaxHeight = 185, CanUserSortColumns = false };
        grid.Columns.Add(new DataGridTextColumn { Header = "Изделие", Binding = new Binding("Description"),
            Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 130 });
        grid.Columns.Add(new DataGridTextColumn { Header = "Кол-во", Binding = new Binding("QuantityText"), Width = 80 });
        grid.Columns.Add(new DataGridTextColumn { Header = "Сумма, сомони", Binding = new Binding("AmountText"), Width = 115 });
        w.Fields.Children.Add(EntryWindow.Field("Состав заказа", grid));
        var total = new TextBlock { FontSize = 23, FontFamily = new FontFamily("Georgia"), TextAlignment = TextAlignment.Right };
        void Refresh(int index = -1)
        {
            grid.ItemsSource = lines.ToList();
            grid.SelectedIndex = index;
            total.Text = $"Итого   {Display.Money(lines.Sum(l => l.Amount))} сомони";
        }
        if (editable)
        {
            var tools = new WrapPanel();
            tools.Children.Add(EntryWindow.ButtonFor("+ Позиция", () =>
            {
                var added = EditLine(w, prices);
                if (added == null) return;
                lines.Add(added);
                Refresh(lines.Count - 1);
            }));
            var edit = EntryWindow.ButtonFor("Изменить", () =>
            {
                int i = grid.SelectedIndex;
                if (i < 0) return;
                var changed = EditLine(w, prices, lines[i]);
                if (changed == null) return;
                lines[i] = changed;
                Refresh(i);
            });
            var remove = EntryWindow.ButtonFor("Удалить", () =>
            {
                int i = grid.SelectedIndex;
                if (i < 0) return;
                lines.RemoveAt(i);
                Refresh(Math.Min(i, lines.Count - 1));
            });
            edit.IsEnabled = remove.IsEnabled = false;
            grid.SelectionChanged += (_, _) => edit.IsEnabled = remove.IsEnabled = grid.SelectedIndex >= 0;
            tools.Children.Add(edit);
            tools.Children.Add(remove);
            w.Fields.Children.Add(tools);
        }
        w.Fields.Children.Add(EntryWindow.Highlight(total));
        Refresh();
        if (old != null)
        {
            var history = new ListBox { ItemsSource = old.History, MaxHeight = 110, BorderThickness = new Thickness(0) };
            w.Fields.Children.Add(EntryWindow.Field($"История · {old.Status}", history));
        }
        if (!editable) { w.Run(); return null; }

        PrintOrder? result = null;
        bool Save()
        {
            if (customer.SelectedItem is not Client selected) return w.Error("Выберите или добавьте клиента.", customer);
            if (!DateTime.TryParse(due.Text, Display.Ru, DateTimeStyles.None, out var deadline)
                || deadline.Date < (old?.Created ?? DateTime.Today).Date)
                return w.Error("Срок должен быть не раньше даты регистрации.", due);
            if (lines.Count == 0) return w.Error("Добавьте хотя бы одну позицию.", grid);
            result = old ?? new PrintOrder { Id = nextId, Customer = selected };
            result.Customer = selected;
            result.Due = deadline.Date;
            result.Comment = comment.Text.Trim();
            result.Lines = lines.ToList();
            if (old == null) result.Register();
            return true;
        }
        string Snapshot() => $"{(customer.SelectedItem as Client)?.Id}\n{due.Text}\n{comment.Text}\n" +
            string.Join("\n", lines.Select(l => $"{l.Description}|{l.Formula}"));
        return w.Run("Сохранить заказ", Save, Snapshot) ? result : null;
    }

    public static string? CancellationReason(Window owner)
    {
        var w = new EntryWindow(owner, "Отмена заказа", 470, 320);
        var box = new TextBox { TextWrapping = TextWrapping.Wrap };
        w.Fields.Children.Add(EntryWindow.Field("Причина отмены", box));
        bool Save() => !string.IsNullOrWhiteSpace(box.Text) || w.Error("Укажите причину отмены.", box);
        return w.Run("Отменить заказ", Save, () => box.Text) ? box.Text.Trim() : null;
    }
}
