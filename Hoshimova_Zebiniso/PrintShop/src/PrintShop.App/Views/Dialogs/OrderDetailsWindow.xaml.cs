using System.Windows;
using PrintShop.App.Infrastructure;
using PrintShop.App.ViewModels;
using PrintShop.Core.Models;
using PrintShop.Core.Services;

namespace PrintShop.App.Views.Dialogs;

/// <summary>Карточка заказа: позиции, история статусов, оплаты.</summary>
public partial class OrderDetailsWindow : Window
{
    private readonly Order _order;

    public OrderDetailsWindow(Order order)
    {
        InitializeComponent();
        _order = order;
        // Подписка на событие смены статуса: карточка обновляется автоматически
        _order.StatusChanged += (_, _) => Fill();
        Fill();
    }

    private void Fill()
    {
        var o = _order;
        Title = $"Заказ №{o.Number}";
        NumberText.Text = $"Заказ №{o.Number}";
        StatusText.Text = "Статус: " + o.StatusName;
        DatesText.Text = $"Принят: {o.CreatedAt:dd.MM.yyyy HH:mm}\nСрок выдачи: {o.DueDate:dd.MM.yyyy}" +
                         (o.GetDueState(DateTime.Today) == DueState.Overdue ? "  (просрочен!)" : "");
        CustomerText.Text = o.Customer.GetDisplayName();
        ContactsText.Text = string.Join(", ", new[] { o.Customer.Phone, o.Customer.Email }.Where(s => !string.IsNullOrEmpty(s)));
        ManagerText.Text = "Менеджер: " + (o.Manager?.FullName ?? "—");
        TotalText.Text = $"Сумма: {o.TotalPrice:N2} сом";
        DiscountText.Text = o.Discount > 0 ? $"в т.ч. скидка: {o.Discount:N2} сом" : "";
        PaidText.Text = $"Оплачено: {o.PaidAmount:N2} сом";
        DebtText.Text = o.Debt > 0 ? $"Остаток к оплате: {o.Debt:N2} сом" : "Оплачено полностью";
        CommentText.Text = string.IsNullOrWhiteSpace(o.Comment) ? "" : "Комментарий: " + o.Comment;
        CommentText.Visibility = string.IsNullOrWhiteSpace(o.Comment) ? Visibility.Collapsed : Visibility.Visible;

        ItemsGrid.ItemsSource = o.Items.ToList();
        HistoryGrid.ItemsSource = o.History.OrderByDescending(h => h.ChangedAt).ToList();
        PaymentsGrid.ItemsSource = o.Payments.OrderByDescending(p => p.PaymentDate).ToList();

        PayButton.IsEnabled = App.Services.Auth.Has(AppPermission.AcceptPayments)
                              && o.Debt > 0 && o.Status != OrderStatus.Cancelled;
    }

    private void Pay_Click(object sender, RoutedEventArgs e)
    {
        Ui.Safe(() =>
        {
            var amount = DialogProvider.Service.AskAmount("Оплата заказа",
                $"Остаток к оплате: {_order.Debt:N2} сом\n\nСумма оплаты:", _order.Debt);
            if (amount == null) return;
            App.Services.Orders.AddPayment(_order, amount.Value);
            Fill();
        });
    }

    private void Receipt_Click(object sender, RoutedEventArgs e) => DialogProvider.Service.ShowReceipt(_order);
}
