namespace RestaurantApp;

internal sealed class MainForm : Form
{
    private RestaurantData data;
    private readonly DataGridView tablesGrid = Grid("Столик", "Мест", "Статус", "Текущий счёт, сом.");
    private readonly DataGridView linesGrid = Grid("Блюдо", "Цена, сом.", "Количество", "Сумма, сом.");
    private readonly DataGridView menuGrid = Grid("Блюдо", "Цена, сом.");
    private readonly DataGridView historyGrid = Grid("№ заказа", "Столик", "Открыт", "Закрыт", "Статус", "Сумма, сом.");
    private readonly DataGridView historyLinesGrid = Grid("Блюдо", "Цена, сом.", "Количество", "Сумма, сом.");
    private readonly ComboBox dishes = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 255 };
    private readonly NumericUpDown quantity = Number(1, 999);
    private readonly NumericUpDown tableNumber = Number(1, 999);
    private readonly NumericUpDown seats = Number(1, 100);
    private readonly TextBox dishName = new() { Width = 230, MaxLength = 100 };
    private readonly NumericUpDown price = Number(0.01m, 99999, 2);
    private readonly Label orderTitle = Caption("");
    private readonly Label total = Caption("");
    private readonly Label revenue = Caption("");
    private readonly Label dashboard = Caption("");
    private readonly Button openOrder;
    private readonly Button addLine;
    private readonly Button subtractLine;
    private readonly Button payOrder;
    private readonly Button cancelOrder;
    private bool refreshing;

    public MainForm(RestaurantData data)
    {
        this.data = data;
        Text = "Ресторан — столики и заказы";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1160, 690);
        MinimumSize = new Size(1000, 570);
        Font = new Font("Segoe UI", 10);
        BackColor = Color.FromArgb(240, 244, 242);

        openOrder = Action("Открыть заказ", OpenOrder, primary: true);
        addLine = Action("Добавить блюдо", AddLine, primary: true);
        subtractLine = Action("Убрать 1 порцию", SubtractLine);
        payOrder = Action("Отметить оплаченным", () => CloseOrder(OrderStatus.Paid), primary: true);
        cancelOrder = Action("Отменить заказ", () => CloseOrder(OrderStatus.Cancelled));

        var navigation = new TabControl { Dock = DockStyle.Fill };
        var floor = new TabPage("Столики и заказы");
        var menu = new TabPage("Меню");
        var history = new TabPage("История заказов");
        navigation.TabPages.AddRange(new[] { floor, menu, history });
        Controls.Add(navigation);

        var heading = new Panel { Dock = DockStyle.Top, Height = 76, BackColor = Color.FromArgb(22, 51, 43) };
        var brand = new Label
        {
            Text = "РЕСТОРАН  /  УПРАВЛЕНИЕ ЗАЛОМ", ForeColor = Color.White,
            Font = new Font("Segoe UI", 17, FontStyle.Bold), AutoSize = true, Location = new Point(18, 10)
        };
        dashboard.Location = new Point(20, 45);
        dashboard.ForeColor = Color.FromArgb(208, 228, 216);
        heading.Controls.AddRange(new Control[] { brand, dashboard });
        Controls.Add(heading);

        var floorLayout = TwoColumns(38);
        floor.Controls.Add(floorLayout);
        floorLayout.Controls.Add(Section(tablesGrid,
            Row(Caption("№"), tableNumber, Caption("Мест"), seats,
                Action("Добавить столик", AddTable)),
            Row(openOrder, Action("Удалить столик", RemoveTable))), 0, 0);
        floorLayout.Controls.Add(Section(linesGrid,
            Row(orderTitle),
            Row(dishes, Caption("Кол-во"), quantity, addLine),
            Row(subtractLine, payOrder, cancelOrder),
            Row(total)), 1, 0);

        menu.Controls.Add(Section(menuGrid, Row(Caption("Название"), dishName, Caption("Цена, сом."),
            price, Action("Добавить", AddDish, primary: true), Action("Изменить", EditDish),
            Action("Удалить", RemoveDish))));

        var historyLayout = TwoColumns(64);
        history.Controls.Add(historyLayout);
        historyLayout.Controls.Add(Section(historyGrid, Row(revenue)), 0, 0);
        historyLayout.Controls.Add(Section(historyLinesGrid, Row(Caption("Состав выбранного заказа"))), 1, 0);

        tablesGrid.CurrentCellChanged += (_, _) => { if (!refreshing) ShowCurrentOrder(); };
        linesGrid.CurrentCellChanged += (_, _) => UpdateOrderButtons();
        historyGrid.CurrentCellChanged += (_, _) => { if (!refreshing) ShowHistoryOrder(); };
        menuGrid.CurrentCellChanged += (_, _) =>
        {
            if (!refreshing && menuGrid.CurrentRow?.Tag is Dish dish)
            {
                dishName.Text = dish.Name;
                price.Value = dish.Price;
            }
        };
        RefreshAll();
    }

    private DiningTable? SelectedTable => tablesGrid.CurrentRow?.Tag as DiningTable;

    private Order? CurrentOrder => SelectedTable is { } table ? FindOpenOrder(table.Number) : null;

    private Order? FindOpenOrder(int number) =>
        data.Orders.FirstOrDefault(order => order.TableNumber == number && order.Status == OrderStatus.Open);

    private void AddTable()
    {
        int number = (int)tableNumber.Value;
        if (data.Tables.Any(table => table.Number == number))
        {
            Notice("Столик с таким номером уже есть.");
            return;
        }

        Commit(() => data.Tables.Add(new DiningTable(number, (int)seats.Value)), number);
    }

    private void RemoveTable()
    {
        if (SelectedTable is not { } table) { Notice("Выберите столик."); return; }
        if (CurrentOrder != null) { Notice("Сначала закройте заказ этого столика."); return; }
        if (Confirm($"Удалить столик №{table.Number}?"))
            Commit(() => data.Tables.Remove(table));
    }

    private void OpenOrder()
    {
        if (SelectedTable is not { } table || CurrentOrder != null) return;
        Commit(() => data.Orders.Add(new Order
        {
            Id = data.Orders.Select(order => order.Id).DefaultIfEmpty().Max() + 1,
            TableNumber = table.Number,
            OpenedAt = DateTimeOffset.Now,
            Status = OrderStatus.Open
        }));
    }

    private void AddLine()
    {
        if (CurrentOrder is not { } order) return;
        if (dishes.SelectedItem is not Dish dish) { Notice("Выберите блюдо из меню."); return; }

        int count = (int)quantity.Value;
        int index = order.Lines.FindIndex(line => line.Name == dish.Name && line.Price == dish.Price);
        if (index >= 0 && order.Lines[index].Quantity + count > 999)
        {
            Notice("В одной позиции может быть до 999 порций.");
            return;
        }

        Commit(() =>
        {
            if (index < 0)
                order.Lines.Add(new OrderLine(dish.Name, dish.Price, count));
            else
            {
                var line = order.Lines[index];
                order.Lines[index] = line with { Quantity = line.Quantity + count };
            }
        });
    }

    private void SubtractLine()
    {
        if (CurrentOrder is not { } order || linesGrid.CurrentRow?.Tag is not int index) return;
        var line = order.Lines[index];
        Commit(() =>
        {
            if (line.Quantity == 1)
                order.Lines.RemoveAt(index);
            else
                order.Lines[index] = line with { Quantity = line.Quantity - 1 };
        });
    }

    private void CloseOrder(OrderStatus status)
    {
        if (CurrentOrder is not { } order) return;
        if (status == OrderStatus.Paid && order.Lines.Count == 0) return;
        string question = status == OrderStatus.Paid
            ? $"Подтвердить оплату заказа №{order.Id} на {order.Total:0.00} сом.?"
            : $"Отменить заказ №{order.Id}?";
        if (!Confirm(question)) return;

        Commit(() =>
        {
            order.Status = status;
            order.ClosedAt = DateTimeOffset.Now;
        });
    }

    private void AddDish() => SaveDish(null);

    private void EditDish()
    {
        if (menuGrid.CurrentRow?.Tag is Dish dish)
            SaveDish(dish);
        else
            Notice("Выберите блюдо в таблице.");
    }

    private void SaveDish(Dish? previous)
    {
        string name = dishName.Text.Trim();
        if (name.Length == 0) { Notice("Введите название блюда."); return; }
        if (data.Menu.Any(dish => dish.Id != previous?.Id &&
            string.Equals(dish.Name, name, StringComparison.CurrentCultureIgnoreCase)))
        {
            Notice("Блюдо с таким названием уже есть.");
            return;
        }

        int id = previous?.Id ?? (data.Menu.Select(dish => dish.Id).DefaultIfEmpty().Max() + 1);
        var updated = new Dish(id, name, price.Value);
        Commit(() =>
        {
            if (previous is null)
                data.Menu.Add(updated);
            else
                data.Menu[data.Menu.IndexOf(previous)] = updated;
        }, dishId: id);
    }

    private void RemoveDish()
    {
        if (menuGrid.CurrentRow?.Tag is not Dish dish) { Notice("Выберите блюдо в таблице."); return; }
        if (Confirm($"Удалить «{dish.Name}» из меню?"))
            Commit(() => data.Menu.Remove(dish));
    }

    private void Commit(System.Action change, int? table = null, int? dishId = null)
    {
        int? previousTable = SelectedTable?.Number;
        change();
        try
        {
            RestaurantStore.Save(data);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            data = RestaurantStore.Load();
            RefreshAll(previousTable);
            Notice("Изменение не сохранено.\n" + error.Message);
            return;
        }

        RefreshAll(table ?? previousTable, dishId);
    }

    private void RefreshAll(int? tableNumber = null, int? dishId = null)
    {
        dishId ??= (menuGrid.CurrentRow?.Tag as Dish)?.Id;
        int? dishForOrder = (dishes.SelectedItem as Dish)?.Id;
        int? historyId = (historyGrid.CurrentRow?.Tag as Order)?.Id;

        refreshing = true;
        tablesGrid.Rows.Clear();
        foreach (var table in data.Tables.OrderBy(table => table.Number))
        {
            var order = FindOpenOrder(table.Number);
            AddRow(tablesGrid, table, $"№ {table.Number}", table.Seats,
                order == null ? "Свободен" : "Занят", order == null ? "—" : $"{order.Total:0.00}");
        }

        menuGrid.Rows.Clear();
        var menu = data.Menu.OrderBy(dish => dish.Name).ToArray();
        foreach (var dish in menu)
            AddRow(menuGrid, dish, dish.Name, $"{dish.Price:0.00}");

        dishes.DataSource = menu;
        dishes.DisplayMember = nameof(Dish.Display);
        int dishIndex = Array.FindIndex(menu, dish => dish.Id == dishForOrder);
        dishes.SelectedIndex = dishIndex >= 0 ? dishIndex : menu.Length > 0 ? 0 : -1;

        historyGrid.Rows.Clear();
        foreach (var order in data.Orders.OrderByDescending(order => order.Id))
        {
            AddRow(historyGrid, order, order.Id, $"№ {order.TableNumber}",
                order.OpenedAt.ToLocalTime().ToString("dd.MM.yy HH:mm"),
                order.ClosedAt?.ToLocalTime().ToString("dd.MM.yy HH:mm") ?? "—",
                StatusText(order.Status), $"{order.Total:0.00}");
        }

        SelectRow(tablesGrid, tag => ((DiningTable)tag).Number == tableNumber);
        SelectRow(menuGrid, tag => ((Dish)tag).Id == dishId);
        SelectRow(historyGrid, tag => ((Order)tag).Id == historyId);
        refreshing = false;
        if (menuGrid.CurrentRow?.Tag is Dish selectedDish)
        {
            dishName.Text = selectedDish.Name;
            price.Value = selectedDish.Price;
        }
        else
        {
            dishName.Clear();
            price.Value = price.Minimum;
        }

        int occupied = data.Orders.Count(order => order.Status == OrderStatus.Open);
        dashboard.Text = $"Свободно {data.Tables.Count - occupied}    ·    Занято {occupied}    ·    Всего столиков {data.Tables.Count}";
        decimal paid = data.Orders.Where(order => order.Status == OrderStatus.Paid).Sum(order => order.Total);
        revenue.Text = $"Выручка по оплаченным заказам   {paid:0.00} сом.";
        ShowCurrentOrder();
        ShowHistoryOrder();
    }

    private void ShowCurrentOrder()
    {
        var order = CurrentOrder;
        orderTitle.Text = order != null ? $"Столик №{order.TableNumber}  /  Заказ №{order.Id}"
            : SelectedTable is { } table ? $"Столик №{table.Number} свободен" : "Выберите столик";
        total.Text = order == null ? "Итого —" : $"Итого   {order.Total:0.00} сом.";
        FillLines(linesGrid, order);
        UpdateOrderButtons();
    }

    private void ShowHistoryOrder() => FillLines(historyLinesGrid, historyGrid.CurrentRow?.Tag as Order);

    private void UpdateOrderButtons()
    {
        var order = CurrentOrder;
        openOrder.Enabled = SelectedTable != null && order == null;
        dishes.Enabled = order != null;
        quantity.Enabled = order != null;
        addLine.Enabled = order != null && dishes.Items.Count > 0;
        subtractLine.Enabled = order != null && linesGrid.CurrentRow?.Tag is int;
        payOrder.Enabled = order?.Lines.Count > 0;
        cancelOrder.Enabled = order != null;
    }

    private static void FillLines(DataGridView grid, Order? order)
    {
        int? selectedIndex = grid.CurrentRow?.Tag as int?;
        grid.Rows.Clear();
        if (order == null) return;
        for (int i = 0; i < order.Lines.Count; i++)
        {
            var line = order.Lines[i];
            AddRow(grid, i, line.Name, $"{line.Price:0.00}", line.Quantity, $"{line.Total:0.00}");
        }
        SelectRow(grid, tag => (int)tag == selectedIndex);
    }

    private static string StatusText(OrderStatus status) => status switch
    {
        OrderStatus.Open => "Открыт",
        OrderStatus.Paid => "Оплачен",
        OrderStatus.Cancelled => "Отменён",
        _ => "Неизвестно"
    };

    private static void AddRow(DataGridView grid, object tag, params object[] values) =>
        grid.Rows[grid.Rows.Add(values)].Tag = tag;

    private static void SelectRow(DataGridView grid, Predicate<object> match)
    {
        var row = grid.Rows.Cast<DataGridViewRow>().FirstOrDefault(row => row.Tag != null && match(row.Tag))
            ?? grid.Rows.Cast<DataGridViewRow>().FirstOrDefault();
        if (row == null) return;
        grid.CurrentCell = row.Cells[0];
        row.Selected = true;
    }

    private void Notice(string message) => MessageBox.Show(this, message, "Ресторан",
        MessageBoxButtons.OK, MessageBoxIcon.Information);

    private bool Confirm(string message) => MessageBox.Show(this, message, "Ресторан",
        MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes;

    private static DataGridView Grid(params string[] columns)
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill, BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
            ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            RowHeadersVisible = false, MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ColumnHeadersHeight = 36, EnableHeadersVisualStyles = false
        };
        grid.RowTemplate.Height = 34;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(223, 238, 230);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(22, 51, 43);
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(29, 104, 81);
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 249, 247);

        foreach (string title in columns)
        {
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = title, SortMode = DataGridViewColumnSortMode.NotSortable,
                MinimumWidth = 45, FillWeight = title == "Блюдо" ? 180 : 100
            });
        }

        return grid;
    }

    private static TableLayoutPanel TwoColumns(int leftPercent)
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, leftPercent));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100 - leftPercent));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        return layout;
    }

    private static TableLayoutPanel Section(DataGridView grid, params FlowLayoutPanel[] rows)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, Padding = new Padding(8), ColumnCount = 1, RowCount = rows.Length + 1
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < rows.Length; i++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(rows[i], 0, i);
        }
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(grid, 0, rows.Length);
        return layout;
    }

    private static FlowLayoutPanel Row(params Control[] controls)
    {
        var row = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
        row.Controls.AddRange(controls);
        return row;
    }

    private static NumericUpDown Number(decimal min, decimal max, int decimals = 0) => new()
    {
        Minimum = min, Maximum = max, Value = min, DecimalPlaces = decimals,
        Increment = decimals == 0 ? 1 : 0.5m, Width = decimals == 0 ? 55 : 100
    };

    private static Label Caption(string text) => new()
    {
        Text = text, AutoSize = true, Margin = new Padding(3, 9, 5, 5)
    };

    private static Button Action(string text, System.Action handler, bool primary = false)
    {
        var button = new Button
        {
            Text = text, AutoSize = true, Height = 34, Padding = new Padding(7, 2, 7, 2),
            FlatStyle = FlatStyle.Flat, BackColor = primary ? Color.FromArgb(26, 91, 72) : Color.White,
            ForeColor = primary ? Color.White : Color.FromArgb(22, 51, 43)
        };
        button.Click += (_, _) => handler();
        return button;
    }
}
