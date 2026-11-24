using AutoService.Domain.Enums;
using AutoService.Domain.Exceptions;
using AutoService.Services;
using AutoService.Services.Reports;

namespace AutoService.Tests;

/// <summary>Тесты сценариев на хранилище в памяти.</summary>
public class ServiceLayerTests
{
    private readonly InMemoryUnitOfWorkFactory _db = new();
    private readonly ClientService _clients;
    private readonly CatalogService _catalog;
    private readonly RepairRequestService _requests;
    private readonly ReportService _reports;

    public ServiceLayerTests()
    {
        _clients = new ClientService(_db);
        _catalog = new CatalogService(_db);
        _requests = new RepairRequestService(_db);
        _reports = new ReportService(_db);
    }

    private (int clientId, int carId) RegisterClient(string plate = "0001AA01")
    {
        var client = _clients.AddClient("Каримов Алишер", "+992911111111", null);
        var car = _clients.AddCar(client.Id, "Lada", "Vesta", 2020, plate, null, 50_000);
        return (client.Id, car.Id);
    }

    private int IssuedRequest(int clientId, int carId)
    {
        var service = _catalog.AddService("Замена масла", 0.5m, 200m, Specialization.Locksmith);
        var mechanic = _catalog.AddMechanic("Петров", "+1", Specialization.Locksmith, 4);
        var r = _requests.Create(clientId, carId, "ТО");
        _requests.ChangeStatus(r.Id, RequestStatus.Diagnostics);
        _requests.AddWork(r.Id, service.Id, mechanic.Id);
        _requests.ChangeStatus(r.Id, RequestStatus.AwaitingApproval);
        _requests.ChangeStatus(r.Id, RequestStatus.Approved);
        _requests.ChangeStatus(r.Id, RequestStatus.InProgress);
        _requests.ChangeStatus(r.Id, RequestStatus.Ready);
        _requests.PayCash(r.Id, 1000m);
        return r.Id;
    }

    [Fact]
    public void Request_numbers_are_sequential_per_day()
    {
        var (clientId, carId) = RegisterClient();

        var first = _requests.Create(clientId, carId, "Стук спереди");
        var second = _requests.Create(clientId, carId, "Не заводится");

        Assert.Equal($"{DateTime.Today:yyMMdd}-001", first.Number);
        Assert.Equal($"{DateTime.Today:yyMMdd}-002", second.Number);
    }

    [Fact]
    public void Duplicate_plate_is_rejected()
    {
        RegisterClient("0001AA01");

        Assert.Throws<DomainException>(() => RegisterClient("0001 aa 01"));
    }

    [Fact]
    public void Issued_status_is_set_only_through_payment()
    {
        var (clientId, carId) = RegisterClient();
        var r = _requests.Create(clientId, carId, "ТО");

        Assert.Throws<DomainException>(() => _requests.ChangeStatus(r.Id, RequestStatus.Issued));
    }

    [Fact]
    public void Full_scenario_produces_receipt_and_revenue()
    {
        var (clientId, carId) = RegisterClient();

        var id = IssuedRequest(clientId, carId);

        var request = _requests.Get(id);
        Assert.Equal(RequestStatus.Issued, request.Status);
        var report = _reports.Revenue(DateTime.Today, DateTime.Today);
        Assert.Equal(1, report.Count);
        Assert.Equal(100m, report.Total);
        Assert.Single(_reports.MechanicLoad(DateTime.Today, DateTime.Today));
        Assert.Single(_reports.CarHistory(carId));
    }

    [Fact]
    public void Loyal_discount_after_three_visits()
    {
        var (clientId, carId) = RegisterClient();
        for (var i = 0; i < 3; i++)
            IssuedRequest(clientId, carId);

        var r = _requests.Create(clientId, carId, "Новая проблема");
        var percent = _requests.ApplyLoyalDiscount(r.Id);

        Assert.Equal(5m, percent);
    }

    [Fact]
    public void Wrong_employee_type_is_rejected_as_mechanic()
    {
        var (clientId, carId) = RegisterClient();
        var receptionist = _catalog.AddReceptionist("Саидова", "+2");
        var service = _catalog.AddService("Замена масла", 0.5m, 200m, Specialization.Locksmith);
        var r = _requests.Create(clientId, carId, "ТО", receptionist.Id);

        Assert.Throws<DomainException>(() => _requests.AddWork(r.Id, service.Id, receptionist.Id));
    }
}
