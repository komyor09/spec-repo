using System.Windows;
using PrintShop.App.Infrastructure;
using PrintShop.Core.Models;

namespace PrintShop.App.Views.Dialogs;

public partial class AmountWindow : Window
{
    public AmountWindow(string title, string prompt, decimal defaultValue)
    {
        InitializeComponent();
        Title = title;
        PromptText.Text = prompt;
        AmountBox.Text = Input.Format(defaultValue);
        Loaded += (_, _) => { AmountBox.Focus(); AmountBox.SelectAll(); };
    }

    public decimal Amount { get; private set; }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        Ui.Safe(() =>
        {
            var v = Input.Decimal(AmountBox.Text, "Сумма");
            if (v <= 0) throw new OrderValidationException("Значение должно быть больше нуля.");
            Amount = v;
            DialogResult = true;
        });
    }
}
