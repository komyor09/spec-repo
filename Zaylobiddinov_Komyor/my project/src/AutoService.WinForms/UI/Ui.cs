using System.Reflection;
using AutoService.Domain.Exceptions;

namespace AutoService.WinForms.UI;

/// <summary>Общие помощники интерфейса: сообщения, настройка таблиц, выбор строки.</summary>
public static class Ui
{
    public const string AppTitle = "Автосервис";

    /// <summary>
    /// Нарушение бизнес-правила (DomainException) — предупреждение с понятным текстом.
    /// Любая другая ошибка — сообщение об ошибке.
    /// </summary>
    public static void ShowError(Exception ex)
    {
        if (ex is DomainException)
            MessageBox.Show(ex.Message, AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        else
            MessageBox.Show($"Произошла ошибка:\n{ex.Message}", AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    /// <summary>Выполняет действие; при ошибке показывает сообщение. Возвращает true при успехе.</summary>
    public static bool Try(Action action)
    {
        try
        {
            action();
            return true;
        }
        catch (Exception ex)
        {
            ShowError(ex);
            return false;
        }
    }

    public static void Info(string text) =>
        MessageBox.Show(text, AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);

    public static bool Confirm(string text) =>
        MessageBox.Show(text, AppTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

    /// <summary>
    /// Единая настройка таблиц: только чтение, выбор строки целиком, столбец Id скрыт,
    /// подчёркивания в заголовках заменяются пробелами (свойства «Дата_выдачи» → «Дата выдачи»).
    /// </summary>
    public static DataGridView CreateGrid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = SystemColors.Window,
        };
        grid.DataBindingComplete += (_, _) =>
        {
            foreach (DataGridViewColumn column in grid.Columns)
            {
                column.HeaderText = column.Name.Replace('_', ' ');
                if (column.ValueType == typeof(decimal))
                {
                    column.DefaultCellStyle.Format = "N2";
                    column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
            }
            if (grid.Columns.Contains("Id"))
                grid.Columns["Id"]!.Visible = false;
        };
        return grid;
    }

    /// <summary>Id выбранной строки (у строк, привязанных к объектам со свойством Id).</summary>
    public static int? SelectedId(DataGridView grid)
    {
        var item = grid.CurrentRow?.DataBoundItem;
        var prop = item?.GetType().GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
        return prop?.GetValue(item) as int?;
    }

    public static Button MakeButton(string text, EventHandler onClick, int width = 140)
    {
        var button = new Button { Text = text, Width = width, Height = 30, Margin = new Padding(3) };
        button.Click += onClick;
        return button;
    }

    public static FlowLayoutPanel ButtonBar(params Control[] controls)
    {
        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(4),
            WrapContents = true,
        };
        bar.Controls.AddRange(controls);
        return bar;
    }

    /// <summary>Показывает список объектов в отдельном окне (для отчётов и истории).</summary>
    public static void ShowList(IWin32Window owner, string title, object rows)
    {
        using var form = new Form
        {
            Text = title,
            Width = 900,
            Height = 450,
            StartPosition = FormStartPosition.CenterParent,
        };
        var grid = CreateGrid();
        form.Controls.Add(grid);
        grid.DataSource = rows;
        form.ShowDialog(owner);
    }
}
