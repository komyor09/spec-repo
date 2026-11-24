namespace AutoService.Domain.Exceptions;

/// <summary>На складе недостаточно запасных частей.</summary>
public class NotEnoughStockException : DomainException
{
    public string PartName { get; }
    public int Requested { get; }
    public int Available { get; }

    public NotEnoughStockException(string partName, int requested, int available)
        : base($"Недостаточно запчастей «{partName}» на складе: нужно {requested}, есть {available}.")
    {
        PartName = partName;
        Requested = requested;
        Available = available;
    }
}
