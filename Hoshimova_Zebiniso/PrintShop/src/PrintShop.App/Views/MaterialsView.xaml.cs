using System.Windows.Controls;
using System.Windows.Input;

namespace PrintShop.App.Views;

public partial class MaterialsView : UserControl
{
    public MaterialsView() => InitializeComponent();

    private void Grid_DoubleClick(object sender, MouseButtonEventArgs e) => GridHelper.EditOnDoubleClick(DataContext);
}