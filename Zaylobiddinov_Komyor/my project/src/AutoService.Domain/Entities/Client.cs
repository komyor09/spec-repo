namespace AutoService.Domain.Entities;

/// <summary>Клиент автосервиса — владелец одного или нескольких автомобилей.</summary>
public class Client : Person
{
    private readonly List<Car> _cars = new();

    public string? Email { get; private set; }
    public IReadOnlyList<Car> Cars => _cars;

    protected Client() { }

    public Client(string fullName, string phone, string? email = null) : base(fullName, phone)
    {
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
    }

    /// <summary>Регистрирует автомобиль клиента. Автомобиль создаётся только через клиента.</summary>
    public Car AddCar(string brand, string model, int year, string plateNumber, string? vin, int mileage)
    {
        var car = new Car(this, brand, model, year, plateNumber, vin, mileage);
        _cars.Add(car);
        return car;
    }
}
