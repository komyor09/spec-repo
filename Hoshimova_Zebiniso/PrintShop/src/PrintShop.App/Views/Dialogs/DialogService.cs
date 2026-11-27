using System.Windows;
using Microsoft.Win32;
using PrintShop.App.ViewModels;
using PrintShop.Core.Models;

namespace PrintShop.App.Views.Dialogs;

/// <summary>Реализация IDialogService на WPF-окнах.</summary>
public class DialogService : IDialogService
{
    private static Window? Owner =>
        Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? Application.Current.MainWindow;

    private static bool Show(Window w)
    {
        w.Owner = Owner;
        return w.ShowDialog() == true;
    }

    public Customer? EditCustomer(Customer? customer)
    {
        var w = new CustomerEditWindow(customer);
        return Show(w) ? w.Result : null;
    }

    public Material? EditMaterial(Material? material)
    {
        var w = new MaterialEditWindow(material);
        return Show(w) ? w.Result : null;
    }

    public ProductType? EditProductType(ProductType? productType)
    {
        var w = new ProductTypeEditWindow(productType);
        return Show(w) ? w.Result : null;
    }

    public AdditionalService? EditService(AdditionalService? service)
    {
        var w = new ServiceEditWindow(service);
        return Show(w) ? w.Result : null;
    }

    public (User User, string? Password)? EditUser(User? user)
    {
        var w = new UserEditWindow(user);
        return Show(w) && w.Result != null ? (w.Result, w.Password) : null;
    }

    public bool EditOrder(Order order) => Show(new OrderEditWindow(new OrderEditViewModel(App.Services, order)));

    public void ShowOrderDetails(Order order) => Show(new OrderDetailsWindow(order));

    public void ShowReceipt(Order order) => Show(new ReceiptWindow(order));

    public decimal? AskAmount(string title, string prompt, decimal defaultValue)
    {
        var w = new AmountWindow(title, prompt, defaultValue);
        return Show(w) ? w.Amount : null;
    }

    public string? SaveFile(string filter, string defaultName)
    {
        var dlg = new SaveFileDialog { Filter = filter, FileName = defaultName, AddExtension = true };
        return dlg.ShowDialog(Owner) == true ? dlg.FileName : null;
    }

    public string? OpenFile(string filter)
    {
        var dlg = new OpenFileDialog { Filter = filter, CheckFileExists = true };
        return dlg.ShowDialog(Owner) == true ? dlg.FileName : null;
    }
}
