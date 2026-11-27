namespace PrintShop.Core.Models;

/// <summary>Аргументы события смены статуса заказа.</summary>
public class OrderStatusChangedEventArgs : EventArgs
{
    public OrderStatusChangedEventArgs(OrderStatus oldStatus, OrderStatus newStatus, User? user)
    {
        OldStatus = oldStatus;
        NewStatus = newStatus;
        User = user;
        ChangedAt = DateTime.Now;
    }

    public OrderStatus OldStatus { get; }
    public OrderStatus NewStatus { get; }
    public User? User { get; }
    public DateTime ChangedAt { get; }
}

/// <summary>Заказ на печать.</summary>
public class Order : EntityBase
{
    /// <summary>
    /// Таблица допустимых переходов между статусами.
    /// «Новый» → «Согласование макета» → «В печати» → «Постобработка» → «Готов» → «Выдан»;
    /// отменить можно только до начала печати.
    /// </summary>
    private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
    {
        [OrderStatus.New] = new[] { OrderStatus.LayoutApproval, OrderStatus.Cancelled },
        [OrderStatus.LayoutApproval] = new[] { OrderStatus.Printing, OrderStatus.New, OrderStatus.Cancelled },
        [OrderStatus.Printing] = new[] { OrderStatus.PostProcessing, OrderStatus.Ready },
        [OrderStatus.PostProcessing] = new[] { OrderStatus.Ready },
        [OrderStatus.Ready] = new[] { OrderStatus.Issued },
        [OrderStatus.Issued] = Array.Empty<OrderStatus>(),
        [OrderStatus.Cancelled] = Array.Empty<OrderStatus>(),
    };

    private readonly List<OrderItem> _items = new();
    private readonly List<Payment> _payments = new();
    private readonly List<StatusHistoryEntry> _history = new();
    private string _number = string.Empty;
    private Customer? _customer;
    private DateTime _dueDate = DateTime.Today.AddDays(3);
    private decimal _totalPrice;
    private decimal _discount;

    /// <summary>Событие смены статуса — используется для записи истории и обновления интерфейса.</summary>
    public event EventHandler<OrderStatusChangedEventArgs>? StatusChanged;

    public string Number
    {
        get => _number;
        set => _number = RequireText(value, "Номер заказа", 30);
    }

    public Customer Customer
    {
        get => _customer ?? throw new OrderValidationException("Не выбран клиент.");
        set => _customer = value ?? throw new OrderValidationException("Не выбран клиент.", nameof(Customer));
    }

    public bool HasCustomer => _customer != null;

    public User? Manager { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime DueDate
    {
        get => _dueDate;
        set
        {
            if (value.Date < CreatedAt.Date)
                throw new OrderValidationException("Дата выдачи не может быть раньше даты приёма заказа.", nameof(DueDate));
            _dueDate = value.Date;
        }
    }

    public OrderStatus Status { get; private set; } = OrderStatus.New;

    public string StatusName => EnumNames.Of(Status);

    public decimal TotalPrice
    {
        get => _totalPrice;
        set => _totalPrice = RequireNonNegative(value, "Сумма заказа");
    }

    public decimal Discount
    {
        get => _discount;
        set => _discount = RequireNonNegative(value, "Скидка");
    }

    public string LayoutPath { get; set; } = string.Empty;

    public string Comment { get; set; } = string.Empty;

    public IReadOnlyList<OrderItem> Items => _items;
    public IReadOnlyList<Payment> Payments => _payments;
    public IReadOnlyList<StatusHistoryEntry> History => _history;

    /// <summary>Оплачено всего.</summary>
    public decimal PaidAmount => _payments.Sum(p => p.Amount);

    /// <summary>Остаток задолженности.</summary>
    public decimal Debt => Math.Max(0, TotalPrice - PaidAmount);

    public bool IsPaid => TotalPrice > 0 && Debt == 0;

    /// <summary>Заказ можно редактировать или отменить только до начала печати.</summary>
    public bool CanEdit => Status is OrderStatus.New or OrderStatus.LayoutApproval;

    public bool IsClosed => Status is OrderStatus.Issued or OrderStatus.Cancelled;

    /// <summary>Состояние срока на указанную дату (для подсветки).</summary>
    public DueState GetDueState(DateTime today)
    {
        if (IsClosed || Status == OrderStatus.Ready) return DueState.Closed;
        if (DueDate.Date < today.Date) return DueState.Overdue;
        if (DueDate.Date == today.Date) return DueState.Today;
        return DueState.Normal;
    }

    // ---------------- Позиции ----------------

    public void AddItem(OrderItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureEditable();
        _items.Add(item);
    }

    public void RemoveItem(OrderItem item)
    {
        EnsureEditable();
        _items.Remove(item);
    }

    public void ClearItems()
    {
        EnsureEditable();
        _items.Clear();
    }

    private void EnsureEditable()
    {
        if (!CanEdit)
            throw new OrderValidationException($"Заказ в статусе «{StatusName}» нельзя изменять.");
    }

    // ---------------- Оплата ----------------

    public void AddPayment(Payment payment)
    {
        ArgumentNullException.ThrowIfNull(payment);
        if (Status == OrderStatus.Cancelled)
            throw new OrderValidationException("Нельзя принять оплату по отменённому заказу.");
        if (payment.Amount > Debt)
            throw new OrderValidationException($"Сумма оплаты ({payment.Amount:0.00}) превышает остаток задолженности ({Debt:0.00}).");
        payment.OrderId = Id;
        _payments.Add(payment);
    }

    // ---------------- Статусы ----------------

    public static IReadOnlyList<OrderStatus> GetAllowedTransitions(OrderStatus from) => AllowedTransitions[from];

    public IReadOnlyList<OrderStatus> NextStatuses => AllowedTransitions[Status];

    public bool CanChangeStatus(OrderStatus newStatus) => AllowedTransitions[Status].Contains(newStatus);

    /// <summary>
    /// Смена статуса с проверкой допустимости перехода.
    /// После успешной смены добавляется запись в историю и вызывается событие StatusChanged.
    /// </summary>
    public void ChangeStatus(OrderStatus newStatus, User? user)
    {
        if (!CanChangeStatus(newStatus))
            throw new OrderValidationException(
                $"Недопустимый переход статуса: «{StatusName}» → «{EnumNames.Of(newStatus)}».");

        if (newStatus == OrderStatus.Issued && Debt > 0)
            throw new OrderValidationException($"Нельзя выдать заказ: не оплачен остаток {Debt:0.00} сом.");

        if (newStatus == OrderStatus.Printing && _items.Count == 0)
            throw new OrderValidationException("Нельзя запустить в печать заказ без позиций.");

        var old = Status;
        Status = newStatus;
        var args = new OrderStatusChangedEventArgs(old, newStatus, user);
        _history.Add(new StatusHistoryEntry
        {
            OrderId = Id,
            Status = newStatus,
            ChangedAt = args.ChangedAt,
            UserId = user?.Id,
            UserName = user?.FullName ?? string.Empty
        });
        OnStatusChanged(args);
    }

    protected virtual void OnStatusChanged(OrderStatusChangedEventArgs e) => StatusChanged?.Invoke(this, e);

    // ---------------- Загрузка из БД (без проверок и событий) ----------------

    internal void RestoreState(OrderStatus status, IEnumerable<OrderItem> items,
        IEnumerable<Payment> payments, IEnumerable<StatusHistoryEntry> history)
    {
        // Сначала копируем коллекции: аргументы могут ссылаться на собственные списки заказа
        var newItems = items.ToList();
        var newPayments = payments.ToList();
        var newHistory = history.OrderBy(h => h.ChangedAt).ToList();
        Status = status;
        _items.Clear(); _items.AddRange(newItems);
        _payments.Clear(); _payments.AddRange(newPayments);
        _history.Clear(); _history.AddRange(newHistory);
    }

    /// <summary>Добавляет начальную запись истории при создании заказа.</summary>
    internal void RegisterCreation(User? user)
    {
        if (_history.Count > 0) return;
        _history.Add(new StatusHistoryEntry
        {
            Status = OrderStatus.New,
            ChangedAt = CreatedAt,
            UserId = user?.Id,
            UserName = user?.FullName ?? string.Empty
        });
    }

    public override string ToString() => $"Заказ №{Number}";
}
