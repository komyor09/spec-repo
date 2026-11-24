using AutoService.Domain.Discounts;
using AutoService.Domain.Entities;
using AutoService.Domain.Enums;
using AutoService.Domain.Exceptions;
using AutoService.Domain.Interfaces;

namespace AutoService.Services;

/// <summary>
/// Сценарии работы с заявками. Каждый метод: загрузить заявку → вызвать её метод → сохранить.
/// Сами правила проверяет класс RepairRequest, сервис только координирует.
/// </summary>
public class RepairRequestService
{
    private readonly IUnitOfWorkFactory _uowFactory;

    public RepairRequestService(IUnitOfWorkFactory uowFactory) => _uowFactory = uowFactory;

    public IReadOnlyList<RepairRequest> Search(RequestFilter filter)
    {
        using var uow = _uowFactory.Create();
        return uow.Requests.Search(filter);
    }

    public RepairRequest Get(int requestId)
    {
        using var uow = _uowFactory.Create();
        return uow.Requests.GetById(requestId) ?? throw new NotFoundException("Заявка", requestId);
    }

    public RepairRequest Create(int clientId, int carId, string problem, int? receptionistId = null, int? mileage = null)
    {
        using var uow = _uowFactory.Create();
        var client = uow.Clients.GetById(clientId) ?? throw new NotFoundException("Клиент", clientId);
        var car = client.Cars.FirstOrDefault(c => c.Id == carId)
                  ?? throw new DomainException("У клиента нет такого автомобиля.");

        Receptionist? receptionist = null;
        if (receptionistId is int rid)
            receptionist = uow.Employees.GetById(rid) as Receptionist
                           ?? throw new DomainException("Выбранный сотрудник не является мастером-приёмщиком.");

        if (mileage is int m)
            car.UpdateMileage(m);

        var number = GenerateNumber(uow);
        var request = new RepairRequest(number, client, car, problem, receptionist);
        uow.Requests.Add(request);
        uow.SaveChanges();
        return request;
    }

    // ---------- Работы и запчасти ----------

    public void AddWork(int requestId, int serviceId, int mechanicId) => Execute(requestId, (uow, r) =>
    {
        var service = uow.Services.GetById(serviceId) ?? throw new NotFoundException("Услуга", serviceId);
        var mechanic = uow.Employees.GetById(mechanicId) as Mechanic
                       ?? throw new DomainException("Выбранный сотрудник не является механиком.");
        r.AddWork(service, mechanic);
    });

    public void RemoveWork(int requestId, int workItemId) => Execute(requestId, (_, r) =>
        r.RemoveWork(r.Works.FirstOrDefault(w => w.Id == workItemId)
                     ?? throw new NotFoundException("Работа", workItemId)));

    public void AddPart(int requestId, int sparePartId, int quantity) => Execute(requestId, (uow, r) =>
    {
        var part = uow.SpareParts.GetById(sparePartId) ?? throw new NotFoundException("Запчасть", sparePartId);
        r.AddPart(part, quantity);
    });

    public void RemovePart(int requestId, int partItemId) => Execute(requestId, (_, r) =>
        r.RemovePart(r.Parts.FirstOrDefault(p => p.Id == partItemId)
                     ?? throw new NotFoundException("Запчасть в заявке", partItemId)));

    public void SetDiscount(int requestId, decimal percent) => Execute(requestId, (_, r) =>
        r.ApplyDiscount(percent == 0 ? new NoDiscount() : new PercentDiscount(percent)));

    /// <summary>Скидка постоянного клиента. Возвращает назначенный процент.</summary>
    public decimal ApplyLoyalDiscount(int requestId)
    {
        decimal percent = 0;
        Execute(requestId, (uow, r) =>
        {
            var visits = uow.Requests.CountIssuedForClient(r.ClientId);
            var strategy = new LoyalClientDiscount(visits);
            r.ApplyDiscount(strategy);
            percent = r.DiscountPercent;
        });
        return percent;
    }

    // ---------- Статусы ----------

    /// <summary>Перевод заявки в указанный статус через соответствующий метод заявки.</summary>
    public void ChangeStatus(int requestId, RequestStatus target) => Execute(requestId, (_, r) =>
    {
        switch (target)
        {
            case RequestStatus.Diagnostics: r.StartDiagnostics(); break;
            case RequestStatus.AwaitingApproval: r.SendForApproval(); break;
            case RequestStatus.Approved: r.Approve(); break;
            case RequestStatus.InProgress: r.StartWork(); break;
            case RequestStatus.WaitingForParts: r.WaitForParts(); break;
            case RequestStatus.Ready: r.Complete(); break;
            case RequestStatus.Cancelled: r.Cancel(); break;
            case RequestStatus.Issued:
                throw new DomainException("Чтобы выдать автомобиль, проведите оплату.");
            default:
                throw new InvalidStatusTransitionException(r.Status, target);
        }
    });

    // ---------- Оплата ----------

    public string PayCash(int requestId, decimal received) =>
        Pay(requestId, total => new CashPayment(total, received));

    public string PayCard(int requestId, string cardLast4) =>
        Pay(requestId, total => new CardPayment(total, cardLast4));

    private string Pay(int requestId, Func<decimal, Payment> createPayment)
    {
        string receipt = string.Empty;
        Execute(requestId, (_, r) =>
        {
            var payment = createPayment(r.Total);
            r.Pay(payment);
            receipt = payment.GetReceiptLine();
        });
        return receipt;
    }

    // ---------- Вспомогательное ----------

    private void Execute(int requestId, Action<IUnitOfWork, RepairRequest> action)
    {
        using var uow = _uowFactory.Create();
        var request = uow.Requests.GetById(requestId) ?? throw new NotFoundException("Заявка", requestId);
        action(uow, request);
        uow.SaveChanges();
    }

    /// <summary>Номер вида ГГММДД-NNN: дата и порядковый номер заявки за день.</summary>
    private static string GenerateNumber(IUnitOfWork uow)
    {
        var today = DateTime.Today;
        var countToday = uow.Requests.CountCreatedOn(today);
        return $"{today:yyMMdd}-{countToday + 1:D3}";
    }
}
