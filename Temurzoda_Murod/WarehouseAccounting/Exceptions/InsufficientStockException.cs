namespace WarehouseAccounting.Exceptions;

/// <summary>
/// Выбрасывается, когда на складе недостаточно товара для отгрузки.
/// </summary>
public class InsufficientStockException : Exception
{
    public string ItemName { get; }
    public int Available { get; }
    public int Requested { get; }

    public InsufficientStockException(string itemName, int available, int requested)
        : base($"Недостаточно товара «{itemName}»: на складе {available} шт., запрошено {requested} шт.")
    {
        ItemName = itemName;
        Available = available;
        Requested = requested;
    }
}