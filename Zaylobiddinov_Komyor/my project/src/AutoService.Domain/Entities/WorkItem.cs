using AutoService.Domain.Common;

namespace AutoService.Domain.Entities;

/// <summary>
/// Работа в заявке. Создаётся только через RepairRequest.AddWork (композиция).
/// Цена фиксируется на момент добавления, чтобы смена прайса не меняла старые заявки.
/// </summary>
public class WorkItem : Entity
{
    public int RepairRequestId { get; private set; }

    public int ServiceId { get; private set; }
    public Service Service { get; private set; } = null!;

    public int MechanicId { get; private set; }
    public Mechanic Mechanic { get; private set; } = null!;

    public decimal Price { get; private set; }

    protected WorkItem() { }

    internal WorkItem(Service service, Mechanic mechanic)
    {
        Service = service;
        ServiceId = service.Id;
        Mechanic = mechanic;
        MechanicId = mechanic.Id;
        Price = service.Price;
    }
}
