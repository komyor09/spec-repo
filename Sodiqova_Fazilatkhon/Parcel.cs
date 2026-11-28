using CourierDelivery.Exceptions;

namespace CourierDelivery.Models;

/// <summary>
/// Базовый класс посылки. Статус меняется только через методы класса,
/// которые проверяют допустимость перехода.
/// </summary>
public abstract class Parcel
{
    private static int _nextId = 1;

    protected Parcel(string recipient, string address, decimal weightKg)
    {
        if (string.IsNullOrWhiteSpace(recipient))
            throw new ArgumentException("Имя получателя не может быть пустым.", nameof(recipient));
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Адрес не может быть пустым.", nameof(address));
        if (weightKg <= 0)
            throw new ArgumentOutOfRangeException(nameof(weightKg), "Вес должен быть больше нуля.");

        Id = _nextId++;
        Recipient = recipient.Trim();
        Address = address.Trim();
        WeightKg = weightKg;
        Status = DeliveryStatus.Created;
    }

    public int Id { get; }

    public string Recipient { get; }

    public string Address { get; }

    public decimal WeightKg { get; }

    public DeliveryStatus Status { get; private set; }

    public int? CourierId { get; private set; }

    /// <summary>Тип посылки — каждый наследник задаёт свой.</summary>
    public abstract string Type { get; }

    /// <summary>Стоимость доставки — у каждого типа своя формула.</summary>
    public abstract decimal CalculateCost();

    /// <summary>Базовая стоимость: 50 сомони + 10 сомони за каждый килограмм.</summary>
    protected decimal BaseCost => 50m + 10m * WeightKg;

    public void AssignTo(int courierId)
    {
        RequireStatus(DeliveryStatus.Created, "назначить курьера");
        CourierId = courierId;
        Status = DeliveryStatus.Assigned;
    }

    public void PickUp()
    {
        RequireStatus(DeliveryStatus.Assigned, "забрать посылку");
        Status = DeliveryStatus.InTransit;
    }

    public void MarkDelivered()
    {
        RequireStatus(DeliveryStatus.InTransit, "отметить доставку");
        Status = DeliveryStatus.Delivered;
    }

    public void Cancel()
    {
        if (Status is DeliveryStatus.InTransit or DeliveryStatus.Delivered or DeliveryStatus.Cancelled)
            throw new DeliveryStateException($"Нельзя отменить посылку в статусе «{Status.ToRussian()}».");

        Status = DeliveryStatus.Cancelled;
        CourierId = null;
    }

    private void RequireStatus(DeliveryStatus expected, string action)
    {
        if (Status != expected)
            throw new DeliveryStateException(
                $"Нельзя {action}: посылка в статусе «{Status.ToRussian()}», нужен статус «{expected.ToRussian()}».");
    }

    public virtual string GetDescription() =>
        $"[{Id}] {Type}: {Recipient}, {Address}, {WeightKg:F1} кг";

    public override string ToString() =>
        $"{GetDescription()} | {Status.ToRussian()} | {CalculateCost():F2} сомони";
}
