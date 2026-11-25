using System.Globalization;
using WarehouseAccounting.Exceptions;
using WarehouseAccounting.Models;
using WarehouseAccounting.Services;

namespace WarehouseAccounting;

internal static class Program
{
    private static readonly Warehouse Warehouse = new();

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
                    case "1": ShowItems(); break;
                    case "2": AddItem(); break;
                    case "3": Receive(); break;
                    case "4": Ship(); break;
                    case "5": Console.WriteLine(Warehouse.BuildReport()); break;
                    case "6": ShowMovements(); break;
                    case "7": ShowExpired(); break;
                    case "0": running = false; break;
                    default: Console.WriteLine("Неизвестная команда."); break;
                }
            }
            catch (InsufficientStockException ex)
            {
                Console.WriteLine($"Ошибка отгрузки: {ex.Message}");
            }
            catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or InvalidOperationException or ArgumentOutOfRangeException)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }

            Console.WriteLine();
        }

        Console.WriteLine("До свидания!");
    }

    private static void PrintMenu()
    {
        Console.WriteLine("=== Складской учёт ===");
        Console.WriteLine("1 - Показать все товары");
        Console.WriteLine("2 - Добавить товар");
        Console.WriteLine("3 - Оформить приход");
        Console.WriteLine("4 - Оформить расход (отгрузку)");
        Console.WriteLine("5 - Отчёт по складу");
        Console.WriteLine("6 - Журнал движений");
        Console.WriteLine("7 - Просроченные продукты");
        Console.WriteLine("0 - Выход");
        Console.Write("Выбор: ");
    }

    private static void ShowItems()
    {
        if (Warehouse.Items.Count == 0)
        {
            Console.WriteLine("Склад пуст.");
            return;
        }

        foreach (var item in Warehouse.Items)
            Console.WriteLine(item);
    }

    private static void AddItem()
    {
        Console.WriteLine("Тип товара: 1 - Электроника, 2 - Продукты, 3 - Одежда");
        string type = ReadText("Выбор: ");
        string name = ReadText("Название: ");
        decimal price = ReadDecimal("Цена: ");
        int quantity = ReadInt("Количество: ");

        Item item = type switch
        {
            "1" => new Electronics(name, price, quantity, ReadInt("Гарантия (мес.): ")),
            "2" => new Food(name, price, quantity, ReadDate("Срок годности (дд.мм.гггг): ")),
            "3" => new Clothing(name, price, quantity, ReadText("Размер: ")),
            _ => throw new ArgumentException("Неизвестный тип товара.")
        };

        Warehouse.AddItem(item);
        Console.WriteLine($"Добавлено: {item}");
    }

    private static void Receive()
    {
        int id = ReadInt("Id товара: ");
        int quantity = ReadInt("Сколько поступило: ");
        Warehouse.Receive(id, quantity);
        Console.WriteLine("Приход оформлен.");
    }

    private static void Ship()
    {
        int id = ReadInt("Id товара: ");
        int quantity = ReadInt("Сколько отгружено: ");
        Warehouse.Ship(id, quantity);
        Console.WriteLine("Расход оформлен.");
    }

    private static void ShowMovements()
    {
        if (Warehouse.Movements.Count == 0)
        {
            Console.WriteLine("Движений пока не было.");
            return;
        }

        foreach (var m in Warehouse.Movements)
        {
            string type = m.Type == MovementType.Income ? "Приход" : "Расход";
            Console.WriteLine($"{m.Date:dd.MM.yyyy HH:mm} | {type,-7} | {m.ItemName} | {m.Quantity} шт.");
        }
    }

    private static void ShowExpired()
    {
        var expired = Warehouse.GetExpiredFood(DateTime.Today).ToList();

        if (expired.Count == 0)
        {
            Console.WriteLine("Просроченных продуктов нет.");
            return;
        }

        Console.WriteLine("Просроченные продукты:");
        foreach (var food in expired)
            Console.WriteLine($"  {food}");
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
            Console.WriteLine("Введите число (например, 99.90).");
        }
    }

    private static DateTime ReadDate(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            if (DateTime.TryParseExact(Console.ReadLine(), "dd.MM.yyyy",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime value))
                return value;
            Console.WriteLine("Формат даты: дд.мм.гггг");
        }
    }

    // ---------- Демонстрационные данные ----------

    private static void SeedDemoData()
    {
        Warehouse.AddItem(new Electronics("Ноутбук Lenovo", 4500m, 5, 24));
        Warehouse.AddItem(new Electronics("Смартфон Samsung", 1200m, 10, 12));
        Warehouse.AddItem(new Food("Молоко 1л", 1.20m, 40, DateTime.Today.AddDays(-2)));
        Warehouse.AddItem(new Food("Хлеб", 0.80m, 25, DateTime.Today.AddDays(3)));
        Warehouse.AddItem(new Clothing("Футболка", 15m, 30, "M"));
    }
}