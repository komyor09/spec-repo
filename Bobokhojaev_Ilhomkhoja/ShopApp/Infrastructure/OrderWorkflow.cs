using ShopApp.Models;

namespace ShopApp.Infrastructure;

/// <summary>Допустимые переходы между статусами заказа.</summary>
public static class OrderWorkflow
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> Transitions = new()
    {
        [OrderStatus.New] = [OrderStatus.Processing, OrderStatus.Cancelled],
        [OrderStatus.Processing] = [OrderStatus.Shipped, OrderStatus.Cancelled],
        [OrderStatus.Shipped] = [OrderStatus.Delivered],
        [OrderStatus.Delivered] = [],
        [OrderStatus.Cancelled] = []
    };

    public static IReadOnlyList<OrderStatus> NextStatuses(OrderStatus current) => Transitions[current];

    public static bool CanChange(OrderStatus from, OrderStatus to) => Transitions[from].Contains(to);

    /// <summary>Покупатель может сам отменить только новый (ещё не обработанный) заказ.</summary>
    public static bool ClientCanCancel(OrderStatus status) => status == OrderStatus.New;
}
