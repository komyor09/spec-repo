using System.Windows;
using System.Windows.Controls;
using PrintShop.App.Infrastructure;
using PrintShop.App.ViewModels;

namespace PrintShop.App.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();
        _vm = new MainViewModel(App.Services);
        DataContext = _vm;
    }

    /// <summary>true — окно закрыто по кнопке «Выйти» (нужно снова показать окно входа).</summary>
    public bool IsLogout { get; private set; }

    private void Nav_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: NavItem item })
            _vm.Selected = item;
    }

    private void Nav_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: NavItem item } rb && item == _vm.Selected)
            rb.IsChecked = true;
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        if (!Ui.Confirm("Выйти из учётной записи?")) return;
        App.Services.Auth.Logout();
        IsLogout = true;
        Close();
    }
}
