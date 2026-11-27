using System.Windows;
using PrintShop.App.Infrastructure;
using PrintShop.Core.Models;

namespace PrintShop.App.Views.Dialogs;

public partial class CustomerEditWindow : Window
{
    private readonly Customer? _source;

    public CustomerEditWindow(Customer? customer)
    {
        InitializeComponent();
        _source = customer;
        Title = customer == null ? "Новый клиент" : "Редактирование клиента";
        if (customer != null)
        {
            CompanyRadio.IsChecked = customer.Type == CustomerType.Company;
            NameBox.Text = customer.Name;
            PhoneBox.Text = customer.Phone;
            EmailBox.Text = customer.Email;
            AddressBox.Text = customer.Address;
        }
        Loaded += (_, _) => NameBox.Focus();
    }

    /// <summary>Результат редактирования (новый объект нужного типа).</summary>
    public Customer? Result { get; private set; }

    private void Type_Changed(object sender, RoutedEventArgs e)
    {
        if (NameLabel != null)
            NameLabel.Text = CompanyRadio.IsChecked == true ? "Название организации *" : "ФИО *";
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        Ui.Safe(() =>
        {
            // Создаётся объект нужного класса-наследника (полиморфизм: Person/Company)
            var c = Customer.Create(CompanyRadio.IsChecked == true ? CustomerType.Company : CustomerType.Person);
            c.Id = _source?.Id ?? 0;
            c.CreatedAt = _source?.CreatedAt ?? DateTime.Now;
            c.Name = NameBox.Text;
            c.Phone = PhoneBox.Text;
            c.Email = EmailBox.Text;
            c.Address = AddressBox.Text;
            Result = c;
            DialogResult = true;
        });
    }
}
