using AutoService.Domain.Entities;
using AutoService.Domain.Enums;
using AutoService.Domain.Interfaces;

namespace AutoService.Services;

/// <summary>Справочники: прайс-лист услуг, склад запчастей, сотрудники.</summary>
public class CatalogService
{
    private readonly IUnitOfWorkFactory _uowFactory;

    public CatalogService(IUnitOfWorkFactory uowFactory) => _uowFactory = uowFactory;

    // ---------- Услуги ----------
    public IReadOnlyList<Service> GetServices()
    {
        using var uow = _uowFactory.Create();
        return uow.Services.GetAll();
    }

    public Service AddService(string name, decimal normHours, decimal hourRate, Specialization specialization)
    {
        using var uow = _uowFactory.Create();
        var service = new Service(name, normHours, hourRate, specialization);
        uow.Services.Add(service);
        uow.SaveChanges();
        return service;
    }

    public void ChangeTariff(int serviceId, decimal normHours, decimal hourRate)
    {
        using var uow = _uowFactory.Create();
        var service = uow.Services.GetById(serviceId) ?? throw new NotFoundException("Услуга", serviceId);
        service.ChangeTariff(normHours, hourRate);
        uow.SaveChanges();
    }

    // ---------- Склад ----------
    public IReadOnlyList<SparePart> GetParts()
    {
        using var uow = _uowFactory.Create();
        return uow.SpareParts.GetAll();
    }

    public SparePart AddPart(string article, string name, decimal price, int stock)
    {
        using var uow = _uowFactory.Create();
        var part = new SparePart(article, name, price, stock);
        uow.SpareParts.Add(part);
        uow.SaveChanges();
        return part;
    }

    public void ReceiveParts(int partId, int quantity)
    {
        using var uow = _uowFactory.Create();
        var part = uow.SpareParts.GetById(partId) ?? throw new NotFoundException("Запчасть", partId);
        part.Receive(quantity);
        uow.SaveChanges();
    }

    // ---------- Сотрудники ----------
    public IReadOnlyList<Employee> GetEmployees()
    {
        using var uow = _uowFactory.Create();
        return uow.Employees.GetAll();
    }

    public IReadOnlyList<Mechanic> GetMechanics() => GetEmployees().OfType<Mechanic>().ToList();

    public IReadOnlyList<Receptionist> GetReceptionists() => GetEmployees().OfType<Receptionist>().ToList();

    public Mechanic AddMechanic(string fullName, string phone, Specialization specialization, int grade)
    {
        using var uow = _uowFactory.Create();
        var mechanic = new Mechanic(fullName, phone, DateTime.Today, specialization, grade);
        uow.Employees.Add(mechanic);
        uow.SaveChanges();
        return mechanic;
    }

    public Receptionist AddReceptionist(string fullName, string phone)
    {
        using var uow = _uowFactory.Create();
        var receptionist = new Receptionist(fullName, phone, DateTime.Today);
        uow.Employees.Add(receptionist);
        uow.SaveChanges();
        return receptionist;
    }
}
