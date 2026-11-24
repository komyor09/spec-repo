using AutoService.Domain.Common;

namespace AutoService.Domain.Entities;

/// <summary>Запасная часть в заявке. Создаётся только через RepairRequest.AddPart.</summary>
public class PartItem : Entity
{
    public int RepairRequestId { get; private set; }

    public int SparePartId { get; private set; }
    public SparePart SparePart { get; private set; } = null!;

    public int Quantity { get; private set; }
    public decimal PriceAtSale { get; private set; }

    public decimal Sum => PriceAtSale * Quantity;

    protected PartItem() { }

    internal PartItem(SparePart part, int quantity)
    {
        SparePart = part;
        SparePartId = part.Id;
        Quantity = quantity;
        PriceAtSale = part.Price;
    }

    internal void Increase(int quantity) => Quantity += quantity;
}
