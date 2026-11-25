using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Tipografiya;

public partial class MainWindow : Window
{
    private readonly List<Client> clients = new();
    private readonly List<PrintOrder> orders = new();
    private readonly TariffCatalog prices = new();
    private long journalVersion;
    private Client? customerFilter;
    private bool ready;

    private PrintOrder? CurrentOrder => OrderGrid.SelectedItem as PrintOrder;
    private Client? CurrentClient => ClientGrid.SelectedItem as Client;
    private int NextClientId => clients.Select(c => c.Id).DefaultIfEmpty().Max() + 1;
    private int NextOrderId => orders.Select(o => o.Id).DefaultIfEmpty().Max() + 1;

    public MainWindow()
    {
        InitializeComponent();

        bool createdSeparateDatabase;
        try
        {
            createdSeparateDatabase = Database.Initialize();
            ReadJournal();
            prices.Reload();
        }
        catch (Exception ex) when (Database.IsStorageError(ex))
        {
            Loaded += (_, _) =>
            {
                MessageBox.Show(this, $"Не удалось открыть базу данных. {ex.Message}",
                    "Типография", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            };
            return;
        }

        Width = Math.Min(Width, SystemParameters.WorkArea.Width);
        Height = Math.Min(Height, SystemParameters.WorkArea.Height);
        StatusFilter.ItemsSource = new[] { "Все состояния", "Принят", "В печати", "Готов", "Выдан", "Отменён" };
        DueFilter.ItemsSource = new[] { "Любой срок", "Сегодня", "Ближайшие 7 дней", "Просроченные" };
        StatusFilter.SelectedIndex = DueFilter.SelectedIndex = 0;
        ready = true;
        NavOrders.IsChecked = true;
        RefreshOrders();
        RefreshClients();
        RefreshPrices();
        SizeChanged += (_, _) => FitWindow();
        Loaded += (_, _) => FitWindow();
        if (createdSeparateDatabase)
        {
            Loaded += (_, _) => MessageBox.Show(this,
                "Обнаружена база другой версии. В папке «Типографии» создан файл tipografiya-studio.db. " +
                "Старые данные остаются в tipografiya.db.",
                "База данных", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key != Key.F5) return;
            ReloadJournal();
            ReloadPrices();
            e.Handled = true;
        };
    }

    private void ReadJournal()
    {
        var data = Database.LoadJournal();
        clients.Clear();
        clients.AddRange(data.Clients);
        orders.Clear();
        orders.AddRange(data.Orders);
        journalVersion = data.Version;
    }

    private bool ReloadJournal()
    {
        int? clientId = CurrentClient?.Id;
        int? orderId = CurrentOrder?.Id;
        int? filterId = customerFilter?.Id;

        try
        {
            ReadJournal();
        }
        catch (Exception ex) when (Database.IsStorageError(ex))
        {
            MessageBox.Show(this, $"Не удалось загрузить журнал. {ex.Message}",
                "База данных", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        customerFilter = clients.Find(c => c.Id == filterId);
        RefreshClients(clients.Find(c => c.Id == clientId));
        RefreshOrders(orders.Find(o => o.Id == orderId));
        return true;
    }

    private bool SaveJournal(IEnumerable<Client> changedClients, params PrintOrder[] changedOrders)
    {
        try
        {
            journalVersion = Database.SaveJournal(journalVersion, changedClients, changedOrders);
            return true;
        }
        catch (Exception ex) when (Database.IsStorageError(ex))
        {
            MessageBox.Show(this, $"Изменения не сохранены. {ex.Message}",
                "База данных", MessageBoxButton.OK, MessageBoxImage.Error);
            if (!ReloadJournal())
                Close();
            return false;
        }
    }

    private void FitWindow()
    {
        bool slim = ActualWidth < 960;
        var labels = slim ? Visibility.Collapsed : Visibility.Visible;
        RailWidth.Width = new GridLength(slim ? 68 : 198);
        Brand.Visibility = NavOrdersLabel.Visibility = NavClientsLabel.Visibility = NavPricesLabel.Visibility = labels;
        Metrics.Visibility = ActualHeight < 590 ? Visibility.Collapsed : Visibility.Visible;
    }

    private void Navigate(object sender, RoutedEventArgs e)
    {
        if (!ready) return;
        int page = sender == NavClients ? 1 : sender == NavPrices ? 2 : 0;
        Sections.SelectedIndex = page;
        (PageTitle.Text, MainAction.Content) = page switch
        {
            1 => ("Клиенты", "+ Новый клиент"),
            2 => ("Тарифы", "+ Новый тариф"),
            _ => ("Журнал заказов", "+ Новый заказ")
        };
    }

    private void PrimaryClick(object sender, RoutedEventArgs e)
    {
        switch (Sections.SelectedIndex)
        {
            case 0: NewOrder(sender, e); break;
            case 1: NewClient(sender, e); break;
            case 2: ChangeTariff(null); break;
        }
    }

    private void RefreshOrders(PrintOrder? select = null)
    {
        if (!ready) return;
        select ??= CurrentOrder;
        string search = OrderSearch.Text.Trim();
        DateTime today = DateTime.Today;
        IEnumerable<PrintOrder> result = orders;
        if (customerFilter != null) result = result.Where(o => o.Customer.Id == customerFilter.Id);
        if (search.Length > 0)
            result = result.Where(o => o.Id.ToString().Contains(search)
                || o.CustomerName.Contains(search, StringComparison.OrdinalIgnoreCase));
        if (StatusFilter.SelectedIndex > 0) result = result.Where(o => o.Status == (string)StatusFilter.SelectedItem);
        result = DueFilter.SelectedIndex switch
        {
            1 => result.Where(o => o.Due.Date == today),
            2 => result.Where(o => o.Due.Date >= today && o.Due.Date < today.AddDays(7)),
            3 => result.Where(o => o.Overdue),
            _ => result
        };

        var rows = result.OrderBy(o => o.Due).ThenBy(o => o.Id).ToList();
        OrderGrid.ItemsSource = rows;
        OrderGrid.SelectedItem = select != null && rows.Contains(select) ? select : null;
        JournalInfo.Text = $"Показано заказов: {rows.Count} из {orders.Count}" +
            (customerFilter == null ? "" : $"   ·   {customerFilter.Name}");
        AcceptedCount.Text = orders.Count(o => o.Status == "Принят").ToString();
        PrintingCount.Text = orders.Count(o => o.Status == "В печати").ToString();
        LateCount.Text = orders.Count(o => o.Overdue).ToString();
        Metrics.ToolTip = "Количество по всему журналу";
        EmptyTitle.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        EmptyTitle.Text = orders.Count == 0 ? "Заказов пока нет" : "По этим условиям заказов нет";
        UpdateActions();
    }

    private void RefreshClients(Client? select = null)
    {
        if (!ready) return;
        select ??= CurrentClient;
        string search = ClientSearch.Text.Trim();
        string digits = Display.Digits(search);
        var rows = clients.Where(c => c.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || (digits.Length > 0 && Display.Digits(c.Phone).Contains(digits)))
            .OrderBy(c => c.Name).ThenBy(c => c.Id).ToList();
        ClientGrid.ItemsSource = rows;
        ClientGrid.SelectedItem = select != null && rows.Contains(select) ? select : null;
        EditClientButton.IsEnabled = ClientOrdersButton.IsEnabled = DeleteClientButton.IsEnabled = CurrentClient != null;
    }

    private void RefreshPrices()
    {
        var selectedId = (PriceGrid.SelectedItem as Tariff)?.Id;
        var query = TariffSearch.Text.Trim();
        var rows = prices.Items.Where(t => t.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        PriceGrid.ItemsSource = rows;
        PriceGrid.SelectedItem = rows.FirstOrDefault(t => t.Id == selectedId);
        TariffCount.Text = $"Показано тарифов: {rows.Count} из {prices.Items.Count}";
        EditTariffButton.IsEnabled = DeleteTariffButton.IsEnabled = PriceGrid.SelectedItem is Tariff;
    }

    private void ReloadPrices()
    {
        try { prices.Reload(); }
        catch (Exception ex) when (Database.IsStorageError(ex))
        {
            MessageBox.Show(this, $"Не удалось прочитать сохранённый прайс. {ex.Message}",
                "Тарифы", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        RefreshPrices();
    }

    private void TariffFilterChanged(object sender, TextChangedEventArgs e)
    {
        if (ready) RefreshPrices();
    }

    private void PriceSelected(object sender, SelectionChangedEventArgs e)
    {
        if (ready) EditTariffButton.IsEnabled = DeleteTariffButton.IsEnabled = PriceGrid.SelectedItem is Tariff;
    }

    private void UpdateActions()
    {
        var o = CurrentOrder;
        OpenButton.IsEnabled = BlankButton.IsEnabled = DeleteOrderButton.IsEnabled = o != null;
        CancelButton.IsEnabled = o?.Status == "Принят";
        NextButton.IsEnabled = o?.NextStatus != null;
        NextButton.Content = o?.Status switch
        {
            "В печати" => "Отметить готовым", "Готов" => "Выдать клиенту", _ => "Передать в печать"
        };
    }

    private void FilterChanged(object sender, RoutedEventArgs e) => RefreshOrders();
    private void ClientFilterChanged(object sender, TextChangedEventArgs e) => RefreshClients();
    private void OrderSelected(object sender, SelectionChangedEventArgs e) { if (ready) UpdateActions(); }
    private void ClientSelected(object sender, SelectionChangedEventArgs e)
    {
        if (ready) EditClientButton.IsEnabled = ClientOrdersButton.IsEnabled = DeleteClientButton.IsEnabled = CurrentClient != null;
    }

    private void ClearFilters()
    {
        customerFilter = null;
        OrderSearch.Clear();
        StatusFilter.SelectedIndex = DueFilter.SelectedIndex = 0;
    }
    private void ResetFilters(object sender, RoutedEventArgs e) { ClearFilters(); RefreshOrders(); }

    private void NewClient(object sender, RoutedEventArgs e)
    {
        var client = EditorWindows.EditClient(this, NextClientId);
        if (client == null || !SaveJournal([client])) return;
        clients.Add(client);
        ClientSearch.Clear();
        RefreshClients(client);
    }
    private void EditClient(object sender, RoutedEventArgs e)
    {
        if (CurrentClient is not Client client || EditorWindows.EditClient(this, client.Id, client) == null) return;
        if (!SaveJournal([client])) return;
        RefreshClients(client);
        RefreshOrders();
    }
    private void ShowClientOrders(object sender, RoutedEventArgs e)
    {
        if (CurrentClient is not Client client) return;
        ClearFilters();
        customerFilter = client;
        NavOrders.IsChecked = true;
        RefreshOrders();
    }
    private void ChangeTariff(Tariff? selected)
    {
        if (!EditorWindows.EditTariff(this, prices, selected)) return;
        TariffSearch.Clear();
        RefreshPrices();
        string id = selected?.Id ?? prices.Items.Last().Id;
        PriceGrid.SelectedItem = prices.Items.First(t => t.Id == id);
        PriceGrid.ScrollIntoView(PriceGrid.SelectedItem);
    }

    private void EditPrices(object sender, RoutedEventArgs e)
    {
        if (PriceGrid.SelectedItem is Tariff tariff) ChangeTariff(tariff);
    }

    private void NewOrder(object sender, RoutedEventArgs e)
    {
        int previousClientCount = clients.Count;
        var order = EditorWindows.EditOrder(this, clients, prices, NextOrderId);
        var newClients = clients.Skip(previousClientCount).ToArray();

        PrintOrder[] changedOrders = order == null ? [] : [order];
        if ((changedOrders.Length > 0 || newClients.Length > 0) &&
            !SaveJournal(newClients, changedOrders))
            return;

        RefreshClients();
        if (order == null) return;

        orders.Add(order);
        RefreshOrders(order);
        if (CurrentOrder != order) JournalInfo.Text += $"   ·   № {order.Id} сохранён и скрыт фильтром.";
    }

    private void OpenOrder(object sender, RoutedEventArgs e)
    {
        if (CurrentOrder is not PrintOrder order) return;

        int previousClientCount = clients.Count;
        var editedOrder = EditorWindows.EditOrder(this, clients, prices, order.Id, order);
        var newClients = clients.Skip(previousClientCount).ToArray();

        PrintOrder[] changedOrders = editedOrder == null ? [] : [editedOrder];
        if ((changedOrders.Length > 0 || newClients.Length > 0) &&
            !SaveJournal(newClients, changedOrders))
            return;

        RefreshClients();
        RefreshOrders(order);
    }

    private void OrderDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source &&
            ItemsControl.ContainerFromElement(OrderGrid, source) is DataGridRow) OpenOrder(sender, e);
    }
    private void AdvanceOrder(object sender, RoutedEventArgs e)
    {
        var order = CurrentOrder;
        if (order?.NextStatus is not string next) return;
        if (MessageBox.Show(this, $"Перевести заказ № {order.Id} в состояние «{next}»?",
                "Состояние заказа", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        order.ChangeStatus(next);
        if (!SaveJournal([], order)) return;
        RefreshOrders(order);
    }
    private void CancelOrder(object sender, RoutedEventArgs e)
    {
        var order = CurrentOrder;
        if (order?.Status != "Принят") return;
        string? reason = EditorWindows.CancellationReason(this);
        if (reason == null) return;
        order.ChangeStatus("Отменён", reason);
        if (!SaveJournal([], order)) return;
        RefreshOrders(order);
    }

    private bool DeleteFromJournal(Func<long> delete)
    {
        try
        {
            journalVersion = delete();
            return true;
        }
        catch (Exception ex) when (Database.IsStorageError(ex))
        {
            MessageBox.Show(this, $"Удаление не выполнено. {ex.Message}",
                "Удаление", MessageBoxButton.OK, MessageBoxImage.Warning);
            if (!ReloadJournal()) Close();
            return false;
        }
    }

    private void DeleteOrder(object sender, RoutedEventArgs e)
    {
        if (CurrentOrder is not PrintOrder order) return;
        if (!EditorWindows.ConfirmDelete(this, "Удалить заказ?",
            $"Заказ № {order.Id} · {order.CustomerName}",
            $"Сумма: {order.TotalText} сомони. Состояние: {order.Status}.\n\n" +
            "Заказ будет удалён из журнала вместе с позициями и историей. Карточка клиента останется.")) return;
        if (!DeleteFromJournal(() => Database.DeleteOrder(journalVersion, order.Id))) return;
        orders.Remove(order);
        RefreshOrders();
    }

    private void DeleteClient(object sender, RoutedEventArgs e)
    {
        if (CurrentClient is not Client client) return;
        int count = orders.Count(o => o.Customer.Id == client.Id);
        if (count > 0)
        {
            EditorWindows.DeletionUnavailable(this,
                $"У клиента «{client.Name}» есть заказы: {count}.\n\n" +
                "Откройте «Заказы клиента» и сначала удалите связанные заказы. Отменённые и выданные заказы тоже учитываются.");
            return;
        }
        if (!EditorWindows.ConfirmDelete(this, "Удалить клиента?", client.Name,
            $"Телефон: {client.Phone}.\n\nКарточка клиента будет удалена из списка.")) return;
        if (!DeleteFromJournal(() => Database.DeleteClient(journalVersion, client.Id))) return;
        clients.Remove(client);
        if (customerFilter?.Id == client.Id) customerFilter = null;
        RefreshClients();
        RefreshOrders();
    }

    private void DeleteTariff(object sender, RoutedEventArgs e)
    {
        if (PriceGrid.SelectedItem is not Tariff tariff) return;
        if (tariff.IsBinding)
        {
            EditorWindows.DeletionUnavailable(this,
                "Служебный тариф скрепления нужен для расчёта брошюр. Его можно изменить, но нельзя удалить. " +
                "Если скрепление бесплатное, установите цену 0 в карточке тарифа.");
            return;
        }
        if (!EditorWindows.ConfirmDelete(this, "Удалить тариф?", tariff.Name,
            $"Цена: {tariff.Price} сомони/{tariff.UnitText}.\n\n" +
            "Тариф исчезнет из прайса и списка для новых позиций. Состав и стоимость уже оформленных заказов сохранятся.")) return;
        try { prices.Delete(tariff); }
        catch (Exception ex) when (Database.IsStorageError(ex))
        {
            MessageBox.Show(this, $"Удаление не выполнено. {ex.Message}",
                "Тарифы", MessageBoxButton.OK, MessageBoxImage.Warning);
            ReloadPrices();
            return;
        }
        RefreshPrices();
    }

    private void ShowBlank(object sender, RoutedEventArgs e)
    {
        if (CurrentOrder is not PrintOrder order) return;
        var doc = new FlowDocument
        {
            PageWidth = 793.7, PageHeight = 1122.5, PagePadding = new Thickness(48),
            ColumnWidth = 700, Background = Brushes.White, Foreground = Brushes.Black,
            FontFamily = new FontFamily("Segoe UI"), FontSize = 14
        };
        void Line(string text, bool heading = false)
        {
            var p = new Paragraph(new Run(text)) { Margin = new Thickness(0, 0, 0, 9) };
            if (heading) { p.FontFamily = new FontFamily("Georgia"); p.FontSize = 25; }
            doc.Blocks.Add(p);
        }
        Line($"Бланк заказа № {order.Id}", true);
        Line($"Регистрация   {order.Created:dd.MM.yyyy}     •     Срок   {order.DueText}     •     {order.Status}");
        Line($"{order.Customer.Name}     {order.Customer.Phone}");
        var table = new Table { CellSpacing = 0 };
        foreach (var width in new[] { 250, 65, 215, 110 }) table.Columns.Add(new TableColumn { Width = new GridLength(width) });
        var rows = new TableRowGroup();
        table.RowGroups.Add(rows);
        void Row(bool header, params string[] values)
        {
            var row = new TableRow();
            foreach (string value in values)
            {
                var cell = new TableCell(new Paragraph(new Run(value)) { Margin = new Thickness(0) })
                {
                    Padding = new Thickness(5, 7, 5, 7),
                    BorderThickness = new Thickness(0, 0, 0, 1), BorderBrush = Brushes.LightGray
                };
                if (header) { cell.Background = Brushes.Honeydew; cell.FontWeight = FontWeights.SemiBold; }
                row.Cells.Add(cell);
            }
            rows.Rows.Add(row);
        }
        Row(true, "Услуга / изделие", "Кол-во", "Расчёт", "Сомони");
        foreach (var l in order.Lines) Row(false, l.Description, l.QuantityText, l.Formula, l.AmountText);
        doc.Blocks.Add(table);
        Line($"Итого   {order.TotalText} сомони", true);
        if (order.Comment.Length > 0) Line($"Комментарий: {order.Comment}");
        if (order.Status == "Отменён") Line($"Причина отмены: {order.History.Last().Reason}");
        new Window
        {
            Title = $"Бланк заказа № {order.Id}", Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Width = Math.Min(900, SystemParameters.WorkArea.Width), Height = Math.Min(690, SystemParameters.WorkArea.Height),
            Content = new FlowDocumentReader { Document = doc, ViewingMode = FlowDocumentReaderViewingMode.Page,
                IsTwoPageViewEnabled = false, Zoom = 75 }
        }.ShowDialog();
    }
}
