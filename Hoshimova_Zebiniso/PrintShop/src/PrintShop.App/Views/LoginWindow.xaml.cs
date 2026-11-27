using System.Windows;
using PrintShop.Core.Models;

namespace PrintShop.App.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
    }

    private void Login_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            App.Services.Auth.Login(LoginBox.Text, PasswordBox.Password);
            DialogResult = true;
        }
        catch (OrderValidationException ex)
        {
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
            PasswordBox.Clear();
            PasswordBox.Focus();
        }
    }
}
