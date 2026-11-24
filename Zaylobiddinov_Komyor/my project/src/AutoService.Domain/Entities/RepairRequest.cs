using AutoService.Domain.Common;
using AutoService.Domain.Discounts;
using AutoService.Domain.Enums;
using AutoService.Domain.Exceptions;

namespace AutoService.Domain.Entities;

/// <summary>
/// Заявка на ремонт (заказ-наряд) — центральный класс модели.
/// Все изменения выполняются через методы, которые проверяют бизнес-правила (инкапсуляция).
/// </summary>
public class RepairRequest : Entity
{
    private static readonly RequestStatus[] EditableStatuses =
        [RequestStatus.New, RequestStatus.Diagnostics, RequestStatus.AwaitingApproval];

    private readonly List<WorkItem> _works = new();
    private readonly List<PartItem> _parts = new();

    public string Number { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public string Problem { get; private set; } = string.Empty;
    public RequestStatus Status { get; private set; }
    public decimal DiscountPercent { get; private set; }
    public DateTime? IssuedAt { get; private set; }

    public int ClientId { get; private set; }
    public Client Client { get; private set; } = null!;

    public int CarId { get; private set; }
    public Car Car { get; private set; } = null!;

    public int? ReceptionistId { get; private set; }
    public Receptionist? Receptionist { get; private set; }

    public Payment? Payment { get; private set; }

    public IReadOnlyList<WorkItem> Works => _works;
    public IReadOnlyList<PartItem> Parts => _parts;

    // ---- вычисляемые свойства (в базе не хранятся) ----
    public decimal WorksSum => _works.Sum(w => w.Price);
    public decimal PartsSum => _parts.Sum(p => p.Sum);
    public decimal Subtotal => WorksSum + PartsSum;
    public decimal DiscountAmount => Math.Round(Subtotal * DiscountPercent / 100m, 2);
    public decimal Total => Subtotal - DiscountAmount;
    public bool IsEditable => EditableStatuses.Contains(Status);
    public bool IsClosed => Status is RequestStatus.Issued or RequestStatus.Cancelled;

    protected RepairRequest() { }

    public RepairRequest(string number, Client client, Car car, string problem, Receptionist? receptionist = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(car);
        // сравниваем по ссылке, если владелец загружен, иначе — по внешнему ключу
        var isOwner = car.Client is not null ? ReferenceEquals(car.Client, client) : car.ClientId == client.Id;
        if (!isOwner)
            throw new DomainException("Автомобиль не принадлежит выбранному клиенту.");

        Number = Guard.NotEmpty(number, "Номер заявки");
        Problem = Guard.NotEmpty(problem, "Описание проблемы");
        Client = client;
        ClientId = client.Id;
        Car = car;
        CarId = car.Id;
        Receptionist = receptionist;
        ReceptionistId = receptionist?.Id;
        CreatedAt = DateTime.Now;
        Status = RequestStatus.New;
    }

    // ================= Работы и запчасти =================

    public WorkItem AddWork(Service service, Mechanic mechanic)
    {
        EnsureEditable();
        if (!mechanic.CanPerform(service))
            throw new DomainException(
                $"Механик {mechanic.FullName} ({mechanic.Specialization.ToRussian()}) " +
                $"не может выполнять работы вида «{service.Specialization.ToRussian()}».");

        var work = new WorkItem(service, mechanic);
        _works.Add(work);
        return work;
    }

    public void RemoveWork(WorkItem work)
    {
        EnsureEditable();
        if (!_works.Remove(work))
            throw new DomainException("Работа не найдена в заявке.");
    }

    /// <summary>Добавляет запчасть и сразу списывает её со склада.</summary>
    public PartItem AddPart(SparePart part, int quantity)
    {
        EnsureEditable();
        part.WriteOff(quantity); // выбросит NotEnoughStockException при нехватке

        var existing = _parts.FirstOrDefault(p => p.SparePart == part);
        if (existing is not null)
        {
            existing.Increase(quantity);
            return existing;
        }

        var item = new PartItem(part, quantity);
        _parts.Add(item);
        return item;
    }

    /// <summary>Убирает запчасть из заявки и возвращает её на склад.</summary>
    public void RemovePart(PartItem item)
    {
        EnsureEditable();
        if (!_parts.Remove(item))
            throw new DomainException("Запчасть не найдена в заявке.");
        item.SparePart.Receive(item.Quantity);
    }

    public void ApplyDiscount(IDiscountStrategy strategy)
    {
        if (IsClosed || Status == RequestStatus.Ready)
            throw new DomainException("Скидку можно изменить только до завершения работ.");
        var percent = strategy.GetPercent();
        if (percent is < 0 or > 50)
            throw new DomainException("Скидка должна быть от 0 до 50 %.");
        DiscountPercent = percent;
    }

    // ================= Смена статуса =================

    public void StartDiagnostics() => ChangeStatus(RequestStatus.Diagnostics);

    public void SendForApproval()
    {
        if (_works.Count == 0)
            throw new DomainException("Нельзя отправить на согласование смету без работ.");
        ChangeStatus(RequestStatus.AwaitingApproval);
    }

    public void Approve() => ChangeStatus(RequestStatus.Approved);

    public void StartWork() => ChangeStatus(RequestStatus.InProgress);

    public void WaitForParts() => ChangeStatus(RequestStatus.WaitingForParts);

    public void Complete() => ChangeStatus(RequestStatus.Ready);

    /// <summary>Оплата и выдача автомобиля. Возможна только для готовой заявки и на полную сумму.</summary>
    public void Pay(Payment payment)
    {
        ArgumentNullException.ThrowIfNull(payment);
        EnsureCanMove(RequestStatus.Issued);
        if (payment.Amount != Total)
            throw new DomainException($"Сумма оплаты ({payment.Amount:N2}) должна быть равна итогу заявки ({Total:N2}).");

        payment.Process(); // полиморфный вызов: CashPayment или CardPayment
        Payment = payment;
        ChangeStatus(RequestStatus.Issued);
        IssuedAt = DateTime.Now;
    }

    /// <summary>Отмена заявки: запчасти возвращаются на склад.</summary>
    public void Cancel()
    {
        EnsureCanMove(RequestStatus.Cancelled);
        foreach (var item in _parts)
            item.SparePart.Receive(item.Quantity);
        ChangeStatus(RequestStatus.Cancelled);
    }

    public IReadOnlyList<RequestStatus> AllowedNextStatuses() => StatusRules.NextStatuses(Status);

    // ================= Закрытые методы =================

    private void ChangeStatus(RequestStatus next)
    {
        EnsureCanMove(next);
        Status = next;
    }

    private void EnsureCanMove(RequestStatus next)
    {
        if (!StatusRules.CanMove(Status, next))
            throw new InvalidStatusTransitionException(Status, next);
    }

    private void EnsureEditable()
    {
        if (!IsEditable)
            throw new DomainException(
                $"Заявку в статусе «{Status.ToRussian()}» нельзя изменять: работы и запчасти меняются только до согласования.");
    }
}
