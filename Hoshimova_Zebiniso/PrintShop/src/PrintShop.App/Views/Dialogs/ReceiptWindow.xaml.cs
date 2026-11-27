using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using PrintShop.App.Infrastructure;
using PrintShop.Core.Models;

namespace PrintShop.App.Views.Dialogs;

/// <summary>Квитанция (чек) заказа: предпросмотр и печать.</summary>
public partial class ReceiptWindow : Window
{
    private readonly Order _order;

    public ReceiptWindow(Order order)
    {
        InitializeComponent();
        _order = order;
        Title = $"Квитанция к заказу №{order.Number}";
        Viewer.Document = BuildDocument(order);
    }

    /// <summary>Формирует документ квитанции.</summary>
    public static FlowDocument BuildDocument(Order o)
    {
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 12,
            PagePadding = new Thickness(48),
            ColumnWidth = double.PositiveInfinity
        };

        doc.Blocks.Add(new Paragraph(new Run("ТИПОГРАФИЯ")) { FontSize = 20, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center, Margin = new Thickness(0) });
        doc.Blocks.Add(new Paragraph(new Run("г. Худжанд  •  тел. +992 (3422) 0-00-00")) { TextAlignment = TextAlignment.Center, Foreground = Brushes.Gray });
        doc.Blocks.Add(new Paragraph(new Run($"КВИТАНЦИЯ К ЗАКАЗУ № {o.Number}")) { FontSize = 16, FontWeight = FontWeights.SemiBold, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 12, 0, 12) });

        var info = new Paragraph { LineHeight = 20 };
        void Line(string label, string value)
        {
            info.Inlines.Add(new Run(label + ": ") { Foreground = Brushes.DimGray });
            info.Inlines.Add(new Run(value) { FontWeight = FontWeights.SemiBold });
            info.Inlines.Add(new LineBreak());
        }
        Line("Дата приёма", o.CreatedAt.ToString("dd.MM.yyyy HH:mm"));
        Line("Дата выдачи", o.DueDate.ToString("dd.MM.yyyy"));
        Line("Клиент", o.Customer.GetDisplayName());
        Line("Телефон", o.Customer.Phone);
        Line("Статус", o.StatusName);
        Line("Принял", o.Manager?.FullName ?? "—");
        doc.Blocks.Add(info);

        // Таблица позиций
        var table = new Table { CellSpacing = 0, BorderBrush = Brushes.Gray, BorderThickness = new Thickness(0.5) };
        foreach (var w in new[] { 30.0, 260, 60, 70, 80 })
            table.Columns.Add(new TableColumn { Width = new GridLength(w) });
        var group = new TableRowGroup();
        group.Rows.Add(Row(true, "№", "Наименование", "Тираж", "Скидка", "Сумма"));
        int i = 1;
        foreach (var item in o.Items)
        {
            var name = item.Description + (item.Product.Services.Count > 0 ? $"\nУслуги: {item.ServicesText}" : "");
            group.Rows.Add(Row(false, (i++).ToString(), name, item.Quantity.ToString("N0"), item.Discount.ToString("N2"), item.Price.ToString("N2")));
        }
        table.RowGroups.Add(group);
        doc.Blocks.Add(table);

        var totals = new Paragraph { TextAlignment = TextAlignment.Right, Margin = new Thickness(0, 12, 0, 0), LineHeight = 20 };
        if (o.Discount > 0) totals.Inlines.Add(new Run($"Скидка за тираж: {o.Discount:N2} сом\n"));
        totals.Inlines.Add(new Run($"ИТОГО К ОПЛАТЕ: {o.TotalPrice:N2} сом\n") { FontSize = 14, FontWeight = FontWeights.Bold });
        totals.Inlines.Add(new Run($"Оплачено: {o.PaidAmount:N2} сом\n"));
        totals.Inlines.Add(new Run($"Остаток: {o.Debt:N2} сом") { FontWeight = FontWeights.SemiBold });
        doc.Blocks.Add(totals);

        if (o.Payments.Count > 0)
        {
            var pays = new Paragraph { Margin = new Thickness(0, 10, 0, 0), FontSize = 11, Foreground = Brushes.DimGray };
            pays.Inlines.Add(new Run("Платежи:") { FontWeight = FontWeights.SemiBold });
            foreach (var p in o.Payments)
                pays.Inlines.Add(new Run($"\n{p.PaymentDate:dd.MM.yyyy HH:mm} — {p.PaymentTypeName}: {p.Amount:N2} сом"));
            doc.Blocks.Add(pays);
        }

        doc.Blocks.Add(new Paragraph(new Run("Подпись менеджера: ____________        Подпись клиента: ____________"))
            { Margin = new Thickness(0, 36, 0, 0) });
        doc.Blocks.Add(new Paragraph(new Run($"Квитанция сформирована {DateTime.Now:dd.MM.yyyy HH:mm}"))
            { FontSize = 10, Foreground = Brushes.Gray, TextAlignment = TextAlignment.Right });
        return doc;
    }

    private static TableRow Row(bool header, params string[] cells)
    {
        var row = new TableRow();
        if (header) row.Background = new SolidColorBrush(Color.FromRgb(0xE8, 0xED, 0xF3));
        for (int c = 0; c < cells.Length; c++)
        {
            row.Cells.Add(new TableCell(new Paragraph(new Run(cells[c]))
            {
                Margin = new Thickness(4),
                TextAlignment = c >= 2 ? TextAlignment.Right : TextAlignment.Left,
                FontWeight = header ? FontWeights.SemiBold : FontWeights.Normal
            })
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(0.5)
            });
        }
        return row;
    }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        Ui.Safe(() =>
        {
            var dlg = new PrintDialog();
            if (dlg.ShowDialog() != true) return;
            // Печатаем отдельную копию документа, подогнанную под размер страницы
            var doc = BuildDocument(_order);
            doc.PageWidth = dlg.PrintableAreaWidth;
            doc.PageHeight = dlg.PrintableAreaHeight;
            doc.ColumnWidth = dlg.PrintableAreaWidth;
            dlg.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, $"Квитанция {_order.Number}");
        });
    }
}
