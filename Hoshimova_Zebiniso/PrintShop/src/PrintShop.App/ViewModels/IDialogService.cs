using PrintShop.Core.Models;

namespace PrintShop.App.ViewModels;

/// <summary>
/// Абстракция работы с диалоговыми окнами. ViewModel не создаёт окна напрямую,
/// а обращается к этому интерфейсу (реализация — в слое представления).
/// </summary>
public interface IDialogService
{
    Customer? EditCustomer(Customer? customer);
    Material? EditMaterial(Material? material);
    ProductType? EditProductType(ProductType? productType);
    AdditionalService? EditService(AdditionalService? service);
    (User User, string? Password)? EditUser(User? user);

    /// <summary>Открывает форму заказа; true — заказ сохранён.</summary>
    bool EditOrder(Order order);
    void ShowOrderDetails(Order order);
    void ShowReceipt(Order order);

    decimal? AskAmount(string title, string prompt, decimal defaultValue);
    string? SaveFile(string filter, string defaultName);
    string? OpenFile(string filter);
}

/// <summary>Точка доступа к реализации диалогов (задаётся при запуске приложения).</summary>
public static class DialogProvider
{
    public static IDialogService Service { get; set; } = null!;
}
