using System.Windows.Controls;
using System.Windows.Input;
using PrintShop.App.ViewModels;

namespace PrintShop.App.Views;

public partial class OrdersView : UserControl
{
    public OrdersView() => InitializeComponent();

    private void Grid_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is OrdersViewModel vm && vm.DetailsCommand.CanExecute(null))
            vm.DetailsCommand.Execute(null);
    }
}
