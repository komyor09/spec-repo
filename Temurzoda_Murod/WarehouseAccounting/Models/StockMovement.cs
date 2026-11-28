namespace WarehouseAccounting.Models;

public enum MovementType
{
    Income,   // приход
    Outcome   // расход
}

/// <summary>Запись о движении товара (журнал операций).</summary>
public record StockMovement(
    DateTime Date,
    int ItemId,
    string ItemName,
    MovementType Type,
    int Quantity);