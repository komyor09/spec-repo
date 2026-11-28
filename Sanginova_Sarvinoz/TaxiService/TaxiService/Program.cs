using System.Globalization;
using System.Text;
using System.Text.Json;

namespace TaxiService;

internal class Program
{
    private static readonly string DataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
    private static readonly string DataFile = Path.Combine(DataDirectory, "taxi-data.json");
    private static TaxiCompany company = new();

    private static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;
        Directory.CreateDirectory(DataDirectory);
        LoadData();
        SeedDataIfEmpty();

        while (true)
        {
            Console.Clear();
            Console.WriteLine("======================================");
            Console.WriteLine("          СЛУЖБА ТАКСИ");
            Console.WriteLine("======================================");
            Console.WriteLine("1. Создать заказ");
            Console.WriteLine("2. Просмотреть все заказы");
            Console.WriteLine("3. Изменить статус заказа");
            Console.WriteLine("4. Список водителей");
            Console.WriteLine("5. Добавить водителя");
            Console.WriteLine("6. Список автомобилей");
            Console.WriteLine("7. Добавить автомобиль");
            Console.WriteLine("8. Отчёт по выполненным заказам");
            Console.WriteLine("9. Сохранить данные");
            Console.WriteLine("0. Выход");
            Console.Write("\nВыберите действие: ");

            switch (Console.ReadLine())
            {
                case "1": CreateOrder(); break;
                case "2": ShowOrders(); break;
                case "3": ChangeOrderStatus(); break;
                case "4": ShowDrivers(); break;
                case "5": AddDriver(); break;
                case "6": ShowCars(); break;
                case "7": AddCar(); break;
                case "8": ShowReport(); break;
                case "9": SaveData(); Pause(); break;
                case "0": SaveData(); return;
                default: Console.WriteLine("Неверный пункт меню."); Pause(); break;
            }
        }
    }

    private static void CreateOrder()
    {
        Console.Clear();
        Console.WriteLine("НОВЫЙ ЗАКАЗ\n");
        string customer = ReadRequired("Имя клиента: ");
        string phone = ReadRequired("Телефон клиента: ");
        string from = ReadRequired("Адрес подачи: ");
        string to = ReadRequired("Адрес назначения: ");
        decimal distance = ReadDecimal("Расстояние (км): ", 0.1m);
        decimal price = Math.Round(10m + distance * 3m, 2);

        var availableDriver = company.Drivers.FirstOrDefault(d => d.IsAvailable);
        if (availableDriver is null)
        {
            Console.WriteLine("\nСвободных водителей сейчас нет. Заказ не создан.");
            Pause();
            return;
        }

        var order = new TaxiOrder
        {
            Id = company.Orders.Count == 0 ? 1 : company.Orders.Max(o => o.Id) + 1,
            CustomerName = customer,
            CustomerPhone = phone,
            PickupAddress = from,
            DestinationAddress = to,
            DistanceKm = distance,
            Price = price,
            DriverId = availableDriver.Id,
            CreatedAt = DateTime.Now,
            Status = OrderStatus.Accepted
        };

        company.Orders.Add(order);
        availableDriver.IsAvailable = false;
        SaveData();

        Console.WriteLine($"\nЗаказ №{order.Id} успешно создан.");
        Console.WriteLine($"Водитель: {availableDriver.Name} ({availableDriver.CarModel}, {availableDriver.CarNumber})");
        Console.WriteLine($"Предварительная стоимость: {price:0.00} сомони");
        Pause();
    }

    private static void ShowOrders()
    {
        Console.Clear();
        Console.WriteLine("СПИСОК ЗАКАЗОВ\n");
        if (company.Orders.Count == 0) Console.WriteLine("Заказов пока нет.");
        foreach (var o in company.Orders.OrderByDescending(o => o.CreatedAt))
        {
            var driver = company.Drivers.FirstOrDefault(d => d.Id == o.DriverId);
            Console.WriteLine($"№{o.Id} | {o.CustomerName} | {o.PickupAddress} → {o.DestinationAddress}");
            Console.WriteLine($"   {o.DistanceKm:0.0} км | {o.Price:0.00} сомони | {o.Status} | Водитель: {driver?.Name ?? "не назначен"}");
            Console.WriteLine($"   Дата: {o.CreatedAt:dd.MM.yyyy HH:mm}\n");
        }
        Pause();
    }

    private static void ChangeOrderStatus()
    {
        Console.Clear();
        Console.WriteLine("ИЗМЕНЕНИЕ СТАТУСА ЗАКАЗА\n");
        int id = ReadInt("Введите номер заказа: ", 1);
        var order = company.Orders.FirstOrDefault(o => o.Id == id);
        if (order is null)
        {
            Console.WriteLine("Заказ не найден.");
            Pause();
            return;
        }

        Console.WriteLine("1. Принят\n2. В пути\n3. Выполнен\n4. Отменён");
        int choice = ReadInt("Новый статус: ", 1, 4);
        order.Status = choice switch
        {
            1 => OrderStatus.Accepted,
            2 => OrderStatus.InProgress,
            3 => OrderStatus.Completed,
            _ => OrderStatus.Cancelled
        };

        if (order.Status is OrderStatus.Completed or OrderStatus.Cancelled)
        {
            var driver = company.Drivers.FirstOrDefault(d => d.Id == order.DriverId);
            if (driver is not null) driver.IsAvailable = true;
        }

        SaveData();
        Console.WriteLine("Статус заказа обновлён.");
        Pause();
    }

    private static void ShowDrivers()
    {
        Console.Clear();
        Console.WriteLine("ВОДИТЕЛИ\n");
        foreach (var d in company.Drivers)
            Console.WriteLine($"ID: {d.Id} | {d.Name} | {d.Phone} | {d.CarModel} ({d.CarNumber}) | {(d.IsAvailable ? "Свободен" : "Занят")}");
        if (company.Drivers.Count == 0) Console.WriteLine("Водителей нет.");
        Pause();
    }

    private static void AddDriver()
    {
        Console.Clear();
        Console.WriteLine("ДОБАВЛЕНИЕ ВОДИТЕЛЯ\n");
        var driver = new Driver
        {
            Id = company.Drivers.Count == 0 ? 1 : company.Drivers.Max(d => d.Id) + 1,
            Name = ReadRequired("ФИО водителя: "),
            Phone = ReadRequired("Телефон: "),
            CarModel = ReadRequired("Марка автомобиля: "),
            CarNumber = ReadRequired("Государственный номер: "),
            IsAvailable = true
        };
        company.Drivers.Add(driver);
        SaveData();
        Console.WriteLine("Водитель добавлен.");
        Pause();
    }

    private static void ShowCars()
    {
        Console.Clear();
        Console.WriteLine("АВТОМОБИЛИ\n");
        foreach (var d in company.Drivers)
            Console.WriteLine($"{d.CarModel} | {d.CarNumber} | Водитель: {d.Name}");
        if (company.Drivers.Count == 0) Console.WriteLine("Автомобилей нет.");
        Pause();
    }

    private static void AddCar()
    {
        Console.Clear();
        Console.WriteLine("ДОБАВЛЕНИЕ АВТОМОБИЛЯ ВМЕСТЕ С ВОДИТЕЛЕМ\n");
        AddDriver();
    }

    private static void ShowReport()
    {
        Console.Clear();
        var completed = company.Orders.Where(o => o.Status == OrderStatus.Completed).ToList();
        Console.WriteLine("ОТЧЁТ ПО ВЫПОЛНЕННЫМ ЗАКАЗАМ\n");
        Console.WriteLine($"Всего заказов: {company.Orders.Count}");
        Console.WriteLine($"Выполнено: {completed.Count}");
        Console.WriteLine($"Отменено: {company.Orders.Count(o => o.Status == OrderStatus.Cancelled)}");
        Console.WriteLine($"Выручка: {completed.Sum(o => o.Price):0.00} сомони");
        Console.WriteLine($"Общий пробег выполненных заказов: {completed.Sum(o => o.DistanceKm):0.0} км");
        Pause();
    }

    private static string ReadRequired(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string? value = Console.ReadLine()?.Trim();
            if (!string.IsNullOrWhiteSpace(value)) return value;
            Console.WriteLine("Поле не может быть пустым.");
        }
    }

    private static decimal ReadDecimal(string prompt, decimal min)
    {
        while (true)
        {
            Console.Write(prompt);
            string input = (Console.ReadLine() ?? "").Replace(',', '.');
            if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value) && value >= min)
                return value;
            Console.WriteLine($"Введите число не меньше {min}.");
        }
    }

    private static int ReadInt(string prompt, int min, int max = int.MaxValue)
    {
        while (true)
        {
            Console.Write(prompt);
            if (int.TryParse(Console.ReadLine(), out int value) && value >= min && value <= max) return value;
            Console.WriteLine("Некорректное значение.");
        }
    }

    private static void LoadData()
    {
        if (!File.Exists(DataFile)) return;
        try
        {
            company = JsonSerializer.Deserialize<TaxiCompany>(File.ReadAllText(DataFile)) ?? new TaxiCompany();
        }
        catch
        {
            company = new TaxiCompany();
            Console.WriteLine("Не удалось прочитать файл данных. Будет создана новая база.");
            Pause();
        }
    }

    private static void SaveData()
    {
        Directory.CreateDirectory(DataDirectory);
        File.WriteAllText(DataFile, JsonSerializer.Serialize(company, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static void SeedDataIfEmpty()
    {
        if (company.Drivers.Count > 0) return;
        company.Drivers.AddRange(new[]
        {
            new Driver { Id = 1, Name = "Алишер Каримов", Phone = "+992 900 000 001", CarModel = "Toyota Corolla", CarNumber = "TJ 1234 AB", IsAvailable = true },
            new Driver { Id = 2, Name = "Фаррух Саидов", Phone = "+992 900 000 002", CarModel = "Hyundai Elantra", CarNumber = "TJ 5678 CD", IsAvailable = true },
            new Driver { Id = 3, Name = "Шахбоз Назаров", Phone = "+992 900 000 003", CarModel = "Chevrolet Cobalt", CarNumber = "TJ 9012 EF", IsAvailable = true }
        });
        SaveData();
    }

    private static void Pause()
    {
        Console.WriteLine("\nНажмите любую клавишу для продолжения...");
        Console.ReadKey(true);
    }
}

public class TaxiCompany
{
    public List<Driver> Drivers { get; set; } = new();
    public List<TaxiOrder> Orders { get; set; } = new();
}

public class Driver
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string CarModel { get; set; } = "";
    public string CarNumber { get; set; } = "";
    public bool IsAvailable { get; set; }
}

public class TaxiOrder
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = "";
    public string CustomerPhone { get; set; } = "";
    public string PickupAddress { get; set; } = "";
    public string DestinationAddress { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public decimal Price { get; set; }
    public int DriverId { get; set; }
    public DateTime CreatedAt { get; set; }
    public OrderStatus Status { get; set; }
}

public enum OrderStatus
{
    Accepted,
    InProgress,
    Completed,
    Cancelled
}
