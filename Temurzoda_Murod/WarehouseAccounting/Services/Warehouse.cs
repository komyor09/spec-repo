using System.Text;
using WarehouseAccounting.Interfaces;
using WarehouseAccounting.Models;

namespace WarehouseAccounting.Services;

/// <summary>
/// Склад: хранит товары и журнал движений, выполняет приход, расход и отчёты.
/// Список товаров наружу отдаётся только для чтения — менять его можно лишь через методы класса.
/// </summary>
public class Warehouse : IReport
{
    private readonly List<Item> _items = new();
    private readonly List<StockMovement> _movements = new();

    public IReadOnlyList<Item> Items => _items;

    public IReadOnlyList<StockMovement> Movements => _movements;

    public decimal TotalValue => _items.Sum(i => i.TotalValue);

    public void AddItem(Item item)
    {
        if (_items.Any(i => i.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Товар «{item.Name}» уже есть на складе.");

        _items.Add(item);

        if (item.Quantity > 0)
            Log(item, MovementType.Income, item.Quantity);
    }

    public Item GetById(int id) =>
        _items.FirstOrDefault(i => i.Id == id)
        ?? throw new KeyNotFoundException($"Товар с Id {id} не найден.");

    public void Receive(int id, int quantity)
    {
        var item = GetById(id);
        item.Increase(quantity);
        Log(item, MovementType.Income, quantity);
    }

    public void Ship(int id, int quantity)
    {
        var item = GetById(id);
        item.Decrease(quantity);            // здесь может выбросить InsufficientStockException
        Log(item, MovementType.Outcome, quantity);
    }

    public IEnumerable<Food> GetExpiredFood(DateTime date) =>
        _items.OfType<Food>().Where(f => f.IsExpired(date));

    public string BuildReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Отчёт по складу ===");
        sb.AppendLine($"Всего позиций: {_items.Count}");
        sb.AppendLine($"Общая стоимость: {TotalValue:F2}");
        sb.AppendLine("По категориям:");

        foreach (var group in _items.GroupBy(i => i.Category))
        {
            sb.AppendLine($"  {group.Key}: {group.Count()} поз., на сумму {group.Sum(i => i.TotalValue):F2}");
        }

        return sb.ToString();
    }

    private void Log(Item item, MovementType type, int quantity) =>
        _movements.Add(new StockMovement(DateTime.Now, item.Id, item.Name, type, quantity));
}