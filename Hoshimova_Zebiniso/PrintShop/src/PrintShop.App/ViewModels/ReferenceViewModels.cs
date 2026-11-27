using System.Collections.ObjectModel;
using PrintShop.App.Infrastructure;
using PrintShop.Core.Models;
using PrintShop.Core.Services;

namespace PrintShop.App.ViewModels;

/// <summary>Раздел «Клиенты»: список, поиск, история заказов выбранного клиента.</summary>
public class CustomersViewModel : CrudViewModel<Customer>
{
    private readonly AppServices _app;

    public CustomersViewModel(AppServices app) : base(app.Customers)
    {
        _app = app;
        NewOrderCommand = new RelayCommand(NewOrder, () => Selected != null && app.Auth.Has(AppPermission.EditOrders));
    }

    public override string Title => "Клиенты";
    public override bool CanEdit => _app.Auth.Has(AppPermission.EditCustomers);

    public ObservableCollection<OrderRow> History { get; } = new();
    public string HistorySummary { get; private set; } = "";

    public RelayCommand NewOrderCommand { get; }

    protected override Customer? ShowEditor(Customer? entity) => DialogProvider.Service.EditCustomer(entity);

    protected override bool Matches(Customer c, string text) =>
        Has(c.Name, text) || Has(c.Phone, text) || Has(c.Email, text);

    protected override string Describe(Customer c) => c.Name;

    protected override void OnSelectedChanged()
    {
        History.Clear();
        if (Selected != null)
            foreach (var o in _app.Orders.Search(new OrderFilter { CustomerId = Selected.Id }))
                History.Add(new OrderRow(o));
        HistorySummary = Selected == null
            ? ""
            : $"Заказов: {History.Count}, на сумму {History.Where(h => h.Order.Status != OrderStatus.Cancelled).Sum(h => h.Total):N2} сом, " +
              $"долг: {History.Where(h => h.Order.Status != OrderStatus.Cancelled).Sum(h => h.Debt):N2} сом";
        OnPropertyChanged(nameof(HistorySummary));
    }

    private void NewOrder()
    {
        var order = _app.Orders.CreateDraft();
        order.Customer = Selected!;
        if (DialogProvider.Service.EditOrder(order)) OnSelectedChanged();
    }
}

/// <summary>Раздел «Продукция»: виды продукции (прайс-лист) и дополнительные услуги.</summary>
public class ProductsViewModel : CrudViewModel<ProductType>
{
    private readonly AppServices _app;
    private AdditionalService? _selectedService;

    public ProductsViewModel(AppServices app) : base(app.ProductTypes)
    {
        _app = app;
        AddServiceCommand = new RelayCommand(AddService, () => CanEdit);
        EditServiceCommand = new RelayCommand(EditService, () => CanEdit && SelectedService != null);
        DeleteServiceCommand = new RelayCommand(DeleteService, () => CanEdit && SelectedService != null);
    }

    public override string Title => "Продукция и услуги";
    public override bool CanEdit => _app.Auth.Has(AppPermission.EditReferences);

    public ObservableCollection<AdditionalService> ServicesList { get; } = new();

    public AdditionalService? SelectedService { get => _selectedService; set => Set(ref _selectedService, value); }

    public RelayCommand AddServiceCommand { get; }
    public RelayCommand EditServiceCommand { get; }
    public RelayCommand DeleteServiceCommand { get; }

    /// <summary>Пояснение формул расчёта для каждого вида (показывается в разделе).</summary>
    public string PricingHelp =>
        "Формулы расчёта стоимости печати (без доп. услуг):\n" +
        "• Визитки: базовая цена × тираж × цветность + материал\n" +
        "• Листовки: базовая цена (за A4) × коэф. формата × тираж × цветность + материал\n" +
        "• Буклеты: базовая цена (за страницу A4) × страниц × коэф. формата × тираж × цветность + материал\n" +
        "• Плакаты: базовая цена (за A4) × коэф. формата × тираж × цветность (+20% при тираже < 10) + материал\n" +
        "• Баннеры: базовая цена (за м²) × площадь × тираж + материал\n" +
        "• Наклейки: базовая цена (за A4) × коэф. формата × тираж × цветность (+30% вырубка) + материал\n\n" +
        "Цветность: ч/б ×1,0; 4+0 ×1,6; 4+4 ×2,2.   Скидка за тираж: от 500 шт — 5%, от 1000 — 10%, от 5000 — 15%.";

    public override void Refresh()
    {
        base.Refresh();
        var id = SelectedService?.Id;
        ServicesList.Clear();
        foreach (var s in _app.Services.GetAll()) ServicesList.Add(s);
        SelectedService = ServicesList.FirstOrDefault(s => s.Id == id);
    }

    protected override ProductType? ShowEditor(ProductType? entity) => DialogProvider.Service.EditProductType(entity);

    protected override bool Matches(ProductType p, string text) => Has(p.Name, text) || Has(p.KindName, text);

    private void AddService()
    {
        var s = DialogProvider.Service.EditService(null);
        if (s == null) return;
        _app.Services.Add(s);
        Refresh();
    }

    private void EditService()
    {
        var s = DialogProvider.Service.EditService(SelectedService);
        if (s == null) return;
        _app.Services.Update(s);
        Refresh();
    }

    private void DeleteService()
    {
        if (!Ui.Confirm($"Удалить услугу «{SelectedService!.Name}»?")) return;
        _app.Services.Delete(SelectedService.Id);
        Refresh();
    }
}

/// <summary>Раздел «Материалы»: склад, приход, контроль минимального запаса.</summary>
public class MaterialsViewModel : CrudViewModel<Material>
{
    private readonly AppServices _app;
    private bool _onlyLow;

    public MaterialsViewModel(AppServices app) : base(app.Materials)
    {
        _app = app;
        ReceiveCommand = new RelayCommand(Receive, () => CanEdit && Selected != null);
    }

    public override string Title => "Материалы";
    public override bool CanEdit => _app.Auth.Has(AppPermission.EditReferences);

    public bool OnlyLow { get => _onlyLow; set { if (Set(ref _onlyLow, value)) Refresh(); } }

    public string LowStockInfo { get; private set; } = "";

    public RelayCommand ReceiveCommand { get; }

    public override void Refresh()
    {
        base.Refresh();
        var low = Repository.GetAll().Where(m => m.IsBelowMinimum).Select(m => m.Name).ToList();
        LowStockInfo = low.Count == 0 ? "" : "⚠ Ниже минимального запаса: " + string.Join(", ", low);
        OnPropertyChanged(nameof(LowStockInfo));
    }

    protected override Material? ShowEditor(Material? entity) => DialogProvider.Service.EditMaterial(entity);

    protected override bool Matches(Material m, string text) =>
        (!OnlyLow || m.IsBelowMinimum) && (text.Length == 0 || Has(m.Name, text) || Has(m.Format, text));

    /// <summary>Поступление материала на склад.</summary>
    private void Receive()
    {
        var m = Selected!;
        var amount = DialogProvider.Service.AskAmount("Поступление материала",
            $"{m}\nТекущий остаток: {m.Quantity:0.##} {m.Unit}\n\nКоличество поступления ({m.Unit}):", 100);
        if (amount == null) return;
        if (amount <= 0) throw new OrderValidationException("Количество должно быть больше нуля.");
        m.Quantity += amount.Value;
        _app.Materials.Update(m);
        Refresh();
    }
}

/// <summary>Раздел «Пользователи» (только для администратора).</summary>
public class UsersViewModel : CrudViewModel<User>
{
    private readonly AppServices _app;

    public UsersViewModel(AppServices app) : base(app.Users) => _app = app;

    public override string Title => "Пользователи";
    public override bool CanEdit => _app.Auth.Has(AppPermission.ManageUsers);

    private string? _pendingPassword;

    protected override User? ShowEditor(User? entity)
    {
        var result = DialogProvider.Service.EditUser(entity);
        if (result == null) return null;
        _pendingPassword = result.Value.Password;
        return result.Value.User;
    }

    protected override void Save(User entity) => _app.Auth.SaveUser(entity, _pendingPassword);

    protected override void Remove(User entity) => _app.Auth.DeleteUser(entity);

    protected override bool Matches(User u, string text) => Has(u.Login, text) || Has(u.FullName, text);

    protected override string Describe(User u) => u.FullName;
}
