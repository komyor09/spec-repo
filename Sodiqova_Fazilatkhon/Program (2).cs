using System.Globalization;
using CourierDelivery.Exceptions;
using CourierDelivery.Models;
using CourierDelivery.Services;

namespace CourierDelivery;

internal static class Program
{
    private static readonly DeliveryService Service = new();

    private static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        SeedDemoData();

        bool running = true;
        while (running)
        {
            PrintMenu();
            string choice = (Console.ReadLine() ?? string.Empty).Trim();
            Console.WriteLine();

            try
            {
                switch (choice)
                {
                    case "1": ShowCouriers(); break;
                    case "2": AddCourier(); break;
                    case "3": ShowParcels(); break;
                    case "4": AddParcel(); break;
                    case "5": AssignManual(); break;
                    case "6": AssignAuto(); break;
                    case "7": PickUp(); break;
                    case "8": Deliver(); break;
                    case "9": Cancel(); break;
                    case "10": Console.WriteLine(Service.BuildReport()); break;
                    case "11": ShowJournal(); break;
                    case "12": ShowOverdue(); break;
                    case "0": running = false; break;
                    default: Console.WriteLine("Неизвестная команда."); break;
                }
            }
            catch (Exception ex) when (ex is DeliveryStateException or CourierCapacityException
                                       or ArgumentException or KeyNotFoundException or InvalidOperationException)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }

            Console.WriteLine();
        }

        Console.WriteLine("До свидания!");
    }

    private static void PrintMenu()
    {
        Console.WriteLine("=== Курьерская доставка ===");
        Console.WriteLine("1  - Показать курьеров");
        Console.WriteLine("2  - Добавить курьера");
        Console.WriteLine("3  - Показать посылки");
        Console.WriteLine("4  - Создать посылку");
        Console.WriteLine("5  - Назначить курьера вручную");
        Console.WriteLine("6  - Назначить курьера автоматически");
        Console.WriteLine("7  - Курьер забрал посылку");
        Console.WriteLine("8  - Отметить доставку");
        Console.WriteLine("9  - Отменить посылку");
        Console.WriteLine("10 - Отчёт");
        Console.WriteLine("11 - Журнал событий");
        Console.WriteLine("12 - Просроченные экспресс-посылки");
        Console.WriteLine("0  - Выход");
        Console.Write("Выбор: ");
    }

    private static void ShowCouriers()
    {
        if (Service.Couriers.Count == 0)
        {
            Console.WriteLine("Курьеров нет.");
            return;
        }

        foreach (var courier in Service.Couriers)
            Console.WriteLine(courier);
    }

    private static void AddCourier()
    {
        string name = ReadText("Имя курьера: ");
        string vehicle = ReadText("Транспорт (пешком, велосипед, авто...): ");
        decimal maxLoad = ReadDecimal("Грузоподъёмность, кг: ");

        var courier = new Courier(name, vehicle, maxLoad);
        Service.AddCourier(courier);
        Console.WriteLine($"Добавлен: {courier}");
    }

    private static void ShowParcels()
    {
        if (Service.Parcels.Count == 0)
        {
            Console.WriteLine("Посылок нет.");
            return;
        }

        foreach (var parcel in Service.Parcels)
            Console.WriteLine(parcel);
    }

    private static void AddParcel()
    {
        Console.WriteLine("Тип посылки: 1 - Обычная, 2 - Хрупкая (+30%), 3 - Экспресс (+50% и срочный сбор)");
        string type = ReadText("Выбор: ");
        string recipient = ReadText("Получатель: ");
        string address = ReadText("Адрес: ");
        decimal weight = ReadDecimal("Вес, кг: ");

        Parcel parcel = type switch
        {
            "1" => new StandardParcel(recipient, address, weight),
            "2" => new FragileParcel(recipient, address, weight),
            "3" => new ExpressParcel(recipient, address, weight, ReadDateTime("Срок доставки (дд.мм.гггг чч:мм): ")),
            _ => throw new ArgumentException("Неизвестный тип посылки.")
        };

        Service.AddParcel(parcel);
        Console.WriteLine($"Создано: {parcel}");
    }

    private static void AssignManual()
    {
        int parcelId = ReadInt("Id посылки: ");
        int courierId = ReadInt("Id курьера: ");
        Service.Assign(parcelId, courierId);
        Console.WriteLine("Посылка назначена.");
    }

    private static void AssignAuto()
    {
        int parcelId = ReadInt("Id посылки: ");
        var courier = Service.AutoAssign(parcelId);
        Console.WriteLine($"Назначен курьер: {courier.Name}");
    }

    private static void PickUp()
    {
        int parcelId = ReadInt("Id посылки: ");
        Service.PickUp(parcelId);
        Console.WriteLine("Посылка забрана, статус «В пути».");
    }

    private static void Deliver()
    {
        int parcelId = ReadInt("Id посылки: ");
        Service.MarkDelivered(parcelId);
        Console.WriteLine("Доставка отмечена.");
    }

    private static void Cancel()
    {
        int parcelId = ReadInt("Id посылки: ");
        Service.Cancel(parcelId);
        Console.WriteLine("Посылка отменена.");
    }

    private static void ShowJournal()
    {
        if (Service.Journal.Count == 0)
        {
            Console.WriteLine("Событий пока нет.");
            return;
        }

        foreach (var entry in Service.Journal)
            Console.WriteLine(entry);
    }

    private static void ShowOverdue()
    {
        var overdue = Service.OverdueExpress(DateTime.Now).ToList();

        if (overdue.Count == 0)
        {
            Console.WriteLine("Просроченных экспресс-посылок нет.");
            return;
        }

        Console.WriteLine("Просроченные экспресс-посылки:");
        foreach (var parcel in overdue)
            Console.WriteLine($"  {parcel}");
    }

    // ---------- Ввод данных ----------

    private static string ReadText(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string? input = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(input))
                return input;
            Console.WriteLine("Поле не может быть пустым.");
        }
    }

    private static int ReadInt(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            if (int.TryParse(Console.ReadLine(), out int value))
                return value;
            Console.WriteLine("Введите целое число.");
        }
    }

    private static decimal ReadDecimal(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string? input = Console.ReadLine()?.Replace(',', '.');
            if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value))
                return value;
            Console.WriteLine("Введите число (например, 2.5).");
        }
    }

    private static DateTime ReadDateTime(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            if (DateTime.TryParseExact(Console.ReadLine(), "dd.MM.yyyy HH:mm",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime value))
                return value;
            Console.WriteLine("Формат: дд.мм.гггг чч:мм");
        }
    }

    // ---------- Демонстрационные данные ----------

    private static void SeedDemoData()
    {
        Service.AddCourier(new Courier("Самир", "велосипед", 20m));
        Service.AddCourier(new Courier("Фарход", "легковой автомобиль", 150m));

        Service.AddParcel(new StandardParcel("Иван Петров", "ул. Ленина, 10", 3m));
        Service.AddParcel(new FragileParcel("Мадина Рахимова", "пр. Сино, 25", 2.5m));
        Service.AddParcel(new ExpressParcel("Ольга Смирнова", "ул. Гагарина, 7", 5m, DateTime.Now.AddHours(2)));
    }
}
