using System.Collections.ObjectModel;
using PrintShop.App.Infrastructure;
using PrintShop.Core.Models;
using PrintShop.Core.Services;

namespace PrintShop.App.ViewModels;

/// <summary>Вариант для выпадающего списка (значение + подпись).</summary>
public record Option<T>(T Value, string Name)
{
    public override string ToString() => Name;
}

/// <summary>Строка таблицы заказов.</summary>
public class OrderRow
{
    public OrderRow(Order order)
    {
        Order = order;
        DueState = order.GetDueState(DateTime.Today);
    }

    public Order Order { get; }
    public string Number => Order.Number;
    public DateTime CreatedAt => Order.CreatedAt;
    public DateTime DueDate => Order.DueDate;
    public string Customer => Order.Customer.Name;
    public string Phone => Order.Customer.Phone;
    public string Products => string.Join(", ", Order.Items.Select(i => i.Product.ProductType.Name).Distinct());
    public string Status => Order.StatusName;
    public decimal Total => Order.TotalPrice;
    public decimal Paid => Order.PaidAmount;
    public decimal Debt => Order.Debt;
    public string Manager => Order.Manager?.FullName ?? "—";
    public DueState DueState { get; }
}

public class OrdersViewModel : SectionViewModel
{
    private readonly AppServices _app;
    private OrderRow? _selected;
    private string _numberText = "";
    private string _customerText = "";
    private Option<OrderStatus?> _statusFilter;
    private DateTime? _from;
    private DateTime? _to;
    private bool _onlyActive;
    private Option<OrderStatus>? _targetStatus;
    private string _summary = "";

    public OrdersViewModel(AppServices app)
    {
        _app = app;
        StatusOptions = new[] { new Option<OrderStatus?>(null, "Все статусы") }
            .Concat(Enum.GetValues<OrderStatus>().Select(s => new Option<OrderStatus?>(s, EnumNames.Of(s))))
            .ToList();
        _statusFilter = StatusOptions[0];

        bool HasSel() => Selected != null;
        NewCommand = new RelayCommand(NewOrder, () => CanEditOrders);
        EditCommand = new RelayCommand(EditOrder, () => CanEditOrders && Selected?.Order.CanEdit == true);
        DetailsCommand = new RelayCommand(Details, HasSel);
        CancelOrderCommand = new RelayCommand(CancelOrder, () => CanEditOrders && Selected?.Order.CanEdit == true);
        ChangeStatusCommand = new RelayCommand(ChangeStatus, () => HasSel() && TargetStatus != null && CanChangeStatus);
        PaymentCommand = new RelayCommand(AddPayment, () => CanAcceptPayments && Selected?.Order.Debt > 0 && Selected.Order.Status != OrderStatus.Cancelled);
        ReceiptCommand = new RelayCommand(() => DialogProvider.Service.ShowReceipt(Selected!.Order), HasSel);
        SearchCommand = new RelayCommand(Refresh);
        ResetCommand = new RelayCommand(ResetFilter);
    }

    public override string Title => "Заказы";

    public ObservableCollection<OrderRow> Orders { get; } = new();
    public ObservableCollection<Option<OrderStatus>> NextStatuses { get; } = new();
    public IReadOnlyList<Option<OrderStatus?>> StatusOptions { get; }

    public bool CanEditOrders => _app.Auth.Has(AppPermission.EditOrders);
    public bool CanChangeStatus => _app.Auth.Has(AppPermission.ChangeStatus);
    public bool CanAcceptPayments => _app.Auth.Has(AppPermission.AcceptPayments);

    public OrderRow? Selected
    {
        get => _selected;
        set
        {
            if (!Set(ref _selected, value)) return;
            NextStatuses.Clear();
            if (value != null)
                foreach (var s in value.Order.NextStatuses.Where(s => s != OrderStatus.Cancelled))
                    NextStatuses.Add(new Option<OrderStatus>(s, EnumNames.Of(s)));
            TargetStatus = NextStatuses.FirstOrDefault();
        }
    }

    public Option<OrderStatus>? TargetStatus { get => _targetStatus; set => Set(ref _targetStatus, value); }

    public string NumberText { get => _numberText; set => Set(ref _numberText, value); }
    public string CustomerText { get => _customerText; set => Set(ref _customerText, value); }
    public Option<OrderStatus?> StatusFilter { get => _statusFilter; set { if (Set(ref _statusFilter, value)) Refresh(); } }
    public DateTime? From { get => _from; set => Set(ref _from, value); }
    public DateTime? To { get => _to; set => Set(ref _to, value); }
    public bool OnlyActive { get => _onlyActive; set { if (Set(ref _onlyActive, value)) Refresh(); } }
    public string Summary { get => _summary; private set => Set(ref _summary, value); }

    public RelayCommand NewCommand { get; }
    public RelayCommand EditCommand { get; }
    public RelayCommand DetailsCommand { get; }
    public RelayCommand CancelOrderCommand { get; }
    public RelayCommand ChangeStatusCommand { get; }
    public RelayCommand PaymentCommand { get; }
    public RelayCommand ReceiptCommand { get; }
    public RelayCommand SearchCommand { get; }
    public RelayCommand ResetCommand { get; }

    public override void Refresh()
    {
        if (From.HasValue && To.HasValue && From > To)
            throw new OrderValidationException("Начальная дата периода больше конечной.");

        var selectedId = Selected?.Order.Id;
        var orders = _app.Orders.Search(new OrderFilter
        {
            Number = NumberText,
            CustomerText = CustomerText,
            Status = StatusFilter.Value,
            From = From,
            To = To,
            OnlyActive = OnlyActive
        });

        Orders.Clear();
        foreach (var o in orders) Orders.Add(new OrderRow(o));
        Selected = Orders.FirstOrDefault(r => r.Order.Id == selectedId);

        int overdue = Orders.Count(r => r.DueState == DueState.Overdue);
        int today = Orders.Count(r => r.DueState == DueState.Today);
        Summary = $"Найдено заказов: {Orders.Count}   •   просрочено: {overdue}   •   срок сегодня: {today}   •   " +
                  $"сумма: {Orders.Sum(r => r.Total):N2} сом   •   долг: {Orders.Where(r => r.Order.Status != OrderStatus.Cancelled).Sum(r => r.Debt):N2} сом";
    }

    private void ResetFilter()
    {
        _numberText = _customerText = "";
        _from = _to = null;
        _onlyActive = false;
        _statusFilter = StatusOptions[0];
        OnPropertyChanged(nameof(NumberText)); OnPropertyChanged(nameof(CustomerText));
        OnPropertyChanged(nameof(From)); OnPropertyChanged(nameof(To));
        OnPropertyChanged(nameof(OnlyActive)); OnPropertyChanged(nameof(StatusFilter));
        Refresh();
    }

    private void NewOrder()
    {
        var order = _app.Orders.CreateDraft();
        if (DialogProvider.Service.EditOrder(order))
        {
            Refresh();
            Selected = Orders.FirstOrDefault(r => r.Order.Id == order.Id);
        }
    }

    private void EditOrder()
    {
        // Редактируем свежую копию из БД, чтобы при отмене не испортить отображаемые данные
        var order = _app.Orders.GetById(Selected!.Order.Id)
                    ?? throw new OrderValidationException("Заказ не найден (возможно, удалён).");
        if (DialogProvider.Service.EditOrder(order)) Refresh();
    }

    private void Details()
    {
        var order = _app.Orders.GetById(Selected!.Order.Id);
        if (order == null) return;
        DialogProvider.Service.ShowOrderDetails(order);
        Refresh();
    }

    private void CancelOrder()
    {
        var order = Selected!.Order;
        if (!Ui.Confirm($"Отменить заказ №{order.Number}?\nОтменённый заказ нельзя будет вернуть в работу.")) return;
        _app.Orders.Cancel(order);
        Refresh();
    }

    private void ChangeStatus()
    {
        var order = Selected!.Order;
        var target = TargetStatus!.Value;
        if (target == OrderStatus.Printing)
        {
            var needs = _app.Orders.GetMaterialNeeds(order);
            var text = string.Join("\n", needs.Select(n => $"  • {n.Material.Name}: {n.Amount:0.##} {n.Material.Unit}"));
            if (!Ui.Confirm($"Перевести заказ №{order.Number} в печать?\nСо склада будут списаны материалы:\n{text}"))
                return;
        }
        _app.Orders.ChangeStatus(order, target);
        Refresh();
    }

    private void AddPayment()
    {
        var order = Selected!.Order;
        var amount = DialogProvider.Service.AskAmount("Оплата заказа",
            $"Заказ №{order.Number}\nСумма заказа: {order.TotalPrice:N2} сом\nОплачено: {order.PaidAmount:N2} сом\nОстаток: {order.Debt:N2} сом\n\nСумма оплаты:",
            order.Debt);
        if (amount == null) return;
        _app.Orders.AddPayment(order, amount.Value);
        Refresh();
    }
}
