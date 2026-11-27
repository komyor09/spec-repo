using PrintShop.Core.Data;
using PrintShop.Core.Models;

namespace PrintShop.Core.Services;

/// <summary>Критерии поиска заказов.</summary>
public class OrderFilter
{
    public string? Number { get; set; }
    public string? CustomerText { get; set; }
    public OrderStatus? Status { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public bool OnlyActive { get; set; }
    public int? CustomerId { get; set; }
}

/// <summary>Бизнес-логика работы с заказами.</summary>
public class OrderService
{
    private readonly DatabaseContext _db;
    private readonly OrderRepository _orders;
    private readonly MaterialRepository _materials;
    private readonly IPriceCalculator _calculator;
    private readonly AuthService _auth;

    public OrderService(DatabaseContext db, OrderRepository orders, MaterialRepository materials,
        IPriceCalculator calculator, AuthService auth)
    {
        _db = db;
        _orders = orders;
        _materials = materials;
        _calculator = calculator;
        _auth = auth;
    }

    public IPriceCalculator Calculator => _calculator;

    /// <summary>Событие: заказы изменились (для обновления окон).</summary>
    public event EventHandler? OrdersChanged;

    public IReadOnlyList<Order> GetAll() => _orders.GetAll();

    public Order? GetById(int id) => _orders.GetById(id);

    /// <summary>Поиск и фильтрация заказов с помощью LINQ.</summary>
    public IReadOnlyList<Order> Search(OrderFilter f)
    {
        IEnumerable<Order> q = _orders.GetAll();

        if (!string.IsNullOrWhiteSpace(f.Number))
            q = q.Where(o => o.Number.Contains(f.Number.Trim(), StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(f.CustomerText))
        {
            var t = f.CustomerText.Trim();
            q = q.Where(o => o.Customer.Name.Contains(t, StringComparison.OrdinalIgnoreCase)
                          || o.Customer.Phone.Contains(t));
        }
        if (f.CustomerId.HasValue) q = q.Where(o => o.Customer.Id == f.CustomerId);
        if (f.Status.HasValue) q = q.Where(o => o.Status == f.Status);
        if (f.From.HasValue) q = q.Where(o => o.CreatedAt.Date >= f.From.Value.Date);
        if (f.To.HasValue) q = q.Where(o => o.CreatedAt.Date <= f.To.Value.Date);
        if (f.OnlyActive) q = q.Where(o => !o.IsClosed);

        return q.OrderByDescending(o => o.CreatedAt).ToList();
    }

    /// <summary>Создание нового (пустого) заказа с номером и датами по умолчанию.</summary>
    public Order CreateDraft()
    {
        return new Order
        {
            Number = _orders.GenerateNumber(DateTime.Now),
            CreatedAt = DateTime.Now,
            DueDate = DateTime.Today.AddDays(3),
            Manager = _auth.CurrentUser
        };
    }

    /// <summary>Проверка заказа перед сохранением.</summary>
    public void Validate(Order order)
    {
        if (!order.HasCustomer)
            throw new OrderValidationException("Выберите клиента.");
        if (order.Items.Count == 0)
            throw new OrderValidationException("Добавьте в заказ хотя бы одну позицию.");
        foreach (var item in order.Items)
            item.Product.CalculateMaterialUsage(); // проверка совместимости формата и материала
    }

    /// <summary>Сохранение (создание или изменение) заказа — в транзакции.</summary>
    public void Save(Order order)
    {
        _auth.Demand(AppPermission.EditOrders);
        if (!order.IsNew && !order.CanEdit)
            throw new OrderValidationException($"Заказ в статусе «{order.StatusName}» нельзя изменить.");
        Validate(order);
        _calculator.Recalculate(order);

        if (order.IsNew)
        {
            order.Manager ??= _auth.CurrentUser;
            order.RegisterCreation(_auth.CurrentUser);
            try
            {
                _db.InTransaction(s => _orders.Add(order, s));
            }
            catch
            {
                // Транзакция откатилась — заказ снова считается несохранённым
                order.Id = 0;
                foreach (var item in order.Items) item.Id = 0;
                foreach (var h in order.History) h.Id = 0;
                throw;
            }
        }
        else
        {
            if (order.PaidAmount > order.TotalPrice)
                throw new OrderValidationException("Сумма заказа стала меньше уже внесённой оплаты.");
            _db.InTransaction(s => _orders.Update(order, s));
        }
        OnOrdersChanged();
    }

    /// <summary>
    /// Смена статуса заказа. При переходе в «В печати» материалы списываются со склада.
    /// Всё выполняется в одной транзакции: при нехватке материала изменения откатываются.
    /// </summary>
    public void ChangeStatus(Order order, OrderStatus newStatus)
    {
        _auth.Demand(newStatus == OrderStatus.Cancelled ? AppPermission.EditOrders : AppPermission.ChangeStatus);
        if (newStatus == OrderStatus.Issued) _auth.Demand(AppPermission.AcceptPayments);

        var oldStatus = order.Status;
        order.ChangeStatus(newStatus, _auth.CurrentUser); // проверка допустимости перехода + событие

        try
        {
            _db.InTransaction(s =>
            {
                if (newStatus == OrderStatus.Printing)
                    WriteOffMaterials(order, s);
                _orders.UpdateStatus(order, s);
            });
        }
        catch
        {
            // Откатываем состояние объекта, т.к. транзакция в БД не прошла
            var reloaded = _orders.GetById(order.Id);
            if (reloaded != null)
                order.RestoreState(reloaded.Status, reloaded.Items, reloaded.Payments, reloaded.History);
            else
                order.RestoreState(oldStatus, order.Items, order.Payments, order.History.Where(h => !h.IsNew));
            throw;
        }
        OnOrdersChanged();
    }

    /// <summary>Расчёт потребности в материалах по заказу (группировка LINQ).</summary>
    public IReadOnlyList<(Material Material, decimal Amount)> GetMaterialNeeds(Order order) =>
        order.Items
            .GroupBy(i => i.Product.Material.Id)
            .Select(g => (g.First().Product.Material, g.Sum(i => i.Product.CalculateMaterialUsage())))
            .ToList();

    private void WriteOffMaterials(Order order, DbSession s)
    {
        foreach (var (material, amount) in GetMaterialNeeds(order))
        {
            var stock = _materials.GetQuantity(material.Id, s);
            if (stock < amount)
                throw new OrderValidationException(
                    $"Недостаточно материала «{material.Name}»: требуется {amount:0.##} {material.Unit}, на складе {stock:0.##} {material.Unit}.");
            _materials.SetQuantity(material.Id, stock - amount, s);
        }
    }

    public void Cancel(Order order)
    {
        if (!order.CanEdit)
            throw new OrderValidationException("Отменить можно только заказ, печать которого ещё не начата.");
        ChangeStatus(order, OrderStatus.Cancelled);
    }

    /// <summary>Приём оплаты по заказу.</summary>
    public Payment AddPayment(Order order, decimal amount, PaymentType? type = null)
    {
        _auth.Demand(AppPermission.AcceptPayments);
        var payment = new Payment
        {
            Amount = amount,
            PaymentDate = DateTime.Now,
            PaymentType = type ?? (amount >= order.Debt ? PaymentType.Final : PaymentType.Prepayment)
        };
        order.AddPayment(payment);
        try
        {
            _db.InTransaction(s => OrderRepository.SavePayment(order, payment, s));
        }
        catch
        {
            order.RestoreState(order.Status, order.Items, order.Payments.Where(p => p != payment), order.History);
            throw;
        }
        OnOrdersChanged();
        return payment;
    }

    protected virtual void OnOrdersChanged() => OrdersChanged?.Invoke(this, EventArgs.Empty);
}
