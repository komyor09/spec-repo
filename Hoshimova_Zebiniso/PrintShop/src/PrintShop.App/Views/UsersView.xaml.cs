using System.Windows.Controls;
using System.Windows.Input;

namespace PrintShop.App.Views;

public partial class UsersView : UserControl
{
    public UsersView() => InitializeComponent();

    private void Grid_DoubleClick(object sender, MouseButtonEventArgs e) => GridHelper.EditOnDoubleClick(DataContext);
}