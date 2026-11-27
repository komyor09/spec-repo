using System.Windows;
using PrintShop.App.Infrastructure;
using PrintShop.App.ViewModels;
using PrintShop.Core.Models;

namespace PrintShop.App.Views.Dialogs;

public partial class UserEditWindow : Window
{
    private readonly User? _source;

    public UserEditWindow(User? user)
    {
        InitializeComponent();
        _source = user;
        Title = user == null ? "Новый пользователь" : "Редактирование пользователя";
        RoleBox.ItemsSource = Enum.GetValues<UserRole>().Select(r => new Option<UserRole>(r, EnumNames.Of(r))).ToList();
        RoleBox.SelectedValue = user?.Role ?? UserRole.Manager;
        if (user != null)
        {
            LoginBox.Text = user.Login;
            NameBox.Text = user.FullName;
            PasswordLabel.Text = "Новый пароль (оставьте пустым, чтобы не менять)";
        }
        Loaded += (_, _) => LoginBox.Focus();
    }

    public User? Result { get; private set; }
    public string? Password { get; private set; }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        Ui.Safe(() =>
        {
            if (PasswordBox.Password != Password2Box.Password)
                throw new OrderValidationException("Пароли не совпадают.");
            if (_source == null && PasswordBox.Password.Length == 0)
                throw new OrderValidationException("Укажите пароль для нового пользователя.");

            Result = new User
            {
                Id = _source?.Id ?? 0,
                Login = LoginBox.Text,
                FullName = NameBox.Text,
                Role = (UserRole)RoleBox.SelectedValue,
                PasswordHash = _source?.PasswordHash ?? ""
            };
            Password = PasswordBox.Password.Length > 0 ? PasswordBox.Password : null;
            DialogResult = true;
        });
    }
}
