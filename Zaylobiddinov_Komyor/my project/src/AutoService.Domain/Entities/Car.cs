using AutoService.Domain.Common;
using AutoService.Domain.Exceptions;

namespace AutoService.Domain.Entities;

/// <summary>Автомобиль клиента.</summary>
public class Car : Entity
{
    public string Brand { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public int Year { get; private set; }
    public string PlateNumber { get; private set; } = string.Empty;
    public string? Vin { get; private set; }
    public int Mileage { get; private set; }

    public int ClientId { get; private set; }
    public Client Client { get; private set; } = null!;

    protected Car() { }

    internal Car(Client owner, string brand, string model, int year, string plateNumber, string? vin, int mileage)
    {
        if (year < 1950 || year > DateTime.Today.Year + 1)
            throw new DomainException($"Некорректный год выпуска: {year}.");
        if (mileage < 0)
            throw new DomainException("Пробег не может быть отрицательным.");

        Client = owner;
        Brand = Guard.NotEmpty(brand, "Марка");
        Model = Guard.NotEmpty(model, "Модель");
        Year = year;
        PlateNumber = Guard.NotEmpty(plateNumber, "Госномер").ToUpperInvariant().Replace(" ", "");
        Vin = string.IsNullOrWhiteSpace(vin) ? null : vin.Trim().ToUpperInvariant();
        Mileage = mileage;
    }

    /// <summary>Обновляет пробег при приёмке. Пробег не может уменьшаться.</summary>
    public void UpdateMileage(int mileage)
    {
        if (mileage < Mileage)
            throw new DomainException($"Новый пробег ({mileage}) меньше прежнего ({Mileage}).");
        Mileage = mileage;
    }

    public override string ToString() => $"{Brand} {Model} ({PlateNumber})";
}
