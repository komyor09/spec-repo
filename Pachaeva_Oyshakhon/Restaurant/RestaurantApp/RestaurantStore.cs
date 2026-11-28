using System.Text.Json;
using System.Text.Json.Serialization;

namespace RestaurantApp;

public enum OrderStatus { Open, Paid, Cancelled }

public sealed record DiningTable(int Number, int Seats);

public sealed record Dish(int Id, string Name, decimal Price)
{
    [JsonIgnore]
    public string Display => $"{Name} · {Price:0.00} сом.";
}

public sealed record OrderLine(string Name, decimal Price, int Quantity)
{
    [JsonIgnore]
    public decimal Total => Price * Quantity;
}

public sealed class Order
{
    public int Id { get; set; }
    public int TableNumber { get; set; }
    public DateTimeOffset OpenedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public OrderStatus Status { get; set; }
    public List<OrderLine> Lines { get; set; } = new();

    [JsonIgnore]
    public decimal Total => Lines.Sum(line => line.Total);
}

public sealed class RestaurantData
{
    public List<DiningTable> Tables { get; set; } = new();
    public List<Dish> Menu { get; set; } = new();
    public List<Order> Orders { get; set; } = new();
}

internal static class RestaurantStore
{
    private static readonly string Folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RestaurantApp");
    private static readonly string DataFile = Path.Combine(Folder, "restaurant.json");
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static RestaurantData Load()
    {
        if (File.Exists(DataFile))
        {
            return JsonSerializer.Deserialize<RestaurantData>(File.ReadAllText(DataFile), Options)
                ?? throw new InvalidDataException("Файл данных пуст.");
        }

        return new RestaurantData
        {
            Tables = Enumerable.Range(1, 8).Select(n => new DiningTable(n, n <= 4 ? 2 : 4)).ToList(),
            Menu = new()
            {
                new Dish(1, "Салат «Свежий»", 18m),
                new Dish(2, "Плов", 35m),
                new Dish(3, "Шашлык из курицы", 28m),
                new Dish(4, "Лагман", 32m),
                new Dish(5, "Чизкейк", 24m),
                new Dish(6, "Чай с лимоном", 8m)
            }
        };
    }

    public static void Save(RestaurantData data)
    {
        Directory.CreateDirectory(Folder);
        string temporaryFile = DataFile + ".tmp";
        File.WriteAllText(temporaryFile, JsonSerializer.Serialize(data, Options));
        File.Move(temporaryFile, DataFile, overwrite: true);
    }
}
