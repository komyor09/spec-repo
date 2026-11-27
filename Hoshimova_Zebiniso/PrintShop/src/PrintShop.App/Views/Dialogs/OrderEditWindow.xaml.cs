using System.Windows;
using PrintShop.App.ViewModels;

namespace PrintShop.App.Views.Dialogs;

public partial class OrderEditWindow : Window
{
    private readonly OrderEditViewModel _vm;

    public OrderEditWindow(OrderEditViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.Save()) DialogResult = true;
    }
}
