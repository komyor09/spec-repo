using AutoService.Domain.Entities;
using AutoService.Domain.Exceptions;
using AutoService.Domain.Interfaces;

namespace AutoService.Services;

/// <summary>Регистрация клиентов и их автомобилей.</summary>
public class ClientService
{
    private readonly IUnitOfWorkFactory _uowFactory;

    public ClientService(IUnitOfWorkFactory uowFactory) => _uowFactory = uowFactory;

    public IReadOnlyList<Client> Search(string? text)
    {
        using var uow = _uowFactory.Create();
        return string.IsNullOrWhiteSpace(text) ? uow.Clients.GetAll() : uow.Clients.Search(text.Trim());
    }

    public Client Get(int id)
    {
        using var uow = _uowFactory.Create();
        return uow.Clients.GetById(id) ?? throw new NotFoundException("Клиент", id);
    }

    public Client AddClient(string fullName, string phone, string? email)
    {
        using var uow = _uowFactory.Create();
        var client = new Client(fullName, phone, email);
        uow.Clients.Add(client);
        uow.SaveChanges();
        return client;
    }

    public Car AddCar(int clientId, string brand, string model, int year, string plateNumber, string? vin, int mileage)
    {
        using var uow = _uowFactory.Create();
        var client = uow.Clients.GetById(clientId) ?? throw new NotFoundException("Клиент", clientId);

        var normalizedPlate = plateNumber.Trim().ToUpperInvariant().Replace(" ", "");
        if (uow.Clients.PlateExists(normalizedPlate))
            throw new DomainException($"Автомобиль с госномером {normalizedPlate} уже зарегистрирован.");

        var car = client.AddCar(brand, model, year, plateNumber, vin, mileage);
        uow.SaveChanges();
        return car;
    }
}
