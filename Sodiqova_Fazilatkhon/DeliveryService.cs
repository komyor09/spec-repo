using System.Text;
using CourierDelivery.Exceptions;
using CourierDelivery.Interfaces;
using CourierDelivery.Models;

namespace CourierDelivery.Services;

/// <summary>
/// Служба доставки: хранит курьеров и посылки, управляет назначениями и статусами,
/// ведёт журнал событий и формирует отчёт.
/// </summary>
public class DeliveryService : IReport
{
    private readonly List<Courier> _couriers = new();
    private readonly List<Parcel> _parcels = new();
    private readonly List<string> _journal = new();

    public IReadOnlyList<Courier> Couriers => _couriers;

    public IReadOnlyList<Parcel> Parcels => _parcels;

    public IReadOnlyList<string> Journal => _journal;

    public decimal Revenue =>
        _parcels.Where(p => p.Status == DeliveryStatus.Delivered).Sum(p => p.CalculateCost());

    public void AddCourier(Courier courier)
    {
        _couriers.Add(courier);
        Log($"Добавлен курьер {courier.Name}");
    }

    public void AddParcel(Parcel parcel)
    {
        _parcels.Add(parcel);
        Log($"Создана посылка #{parcel.Id} для {parcel.Recipient}");
    }

    public Courier GetCourier(int id) =>
        _couriers.FirstOrDefault(c => c.Id == id)
        ?? throw new KeyNotFoundException($"Курьер с Id {id} не найден.");

    public Parcel GetParcel(int id) =>
        _parcels.FirstOrDefault(p => p.Id == id)
        ?? throw new KeyNotFoundException($"Посылка с Id {id} не найдена.");

    public void Assign(int parcelId, int courierId)
    {
        var parcel = GetParcel(parcelId);
        var courier = GetCourier(courierId);
        courier.Take(parcel);
        Log($"Посылка #{parcel.Id} назначена курьеру {courier.Name}");
    }

    /// <summary>Назначает посылку курьеру с самой большой свободной нагрузкой.</summary>
    public Courier AutoAssign(int parcelId)
    {
        var parcel = GetParcel(parcelId);
        var courier = _couriers
            .Where(c => c.CanTake(parcel))
            .OrderByDescending(c => c.FreeCapacityKg)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("Нет курьера с достаточной свободной грузоподъёмностью.");

        Assign(parcelId, courier.Id);
        return courier;
    }

    public void PickUp(int parcelId)
    {
        var parcel = GetParcel(parcelId);
        parcel.PickUp();
        Log($"Посылка #{parcel.Id} забрана курьером");
    }

    public void MarkDelivered(int parcelId)
    {
        var parcel = GetParcel(parcelId);
        parcel.MarkDelivered();

        if (parcel.CourierId is int courierId)
            GetCourier(courierId).Release(parcel);

        Log($"Посылка #{parcel.Id} доставлена получателю {parcel.Recipient}");
    }

    public void Cancel(int parcelId)
    {
        var parcel = GetParcel(parcelId);
        int? courierId = parcel.CourierId;

        parcel.Cancel();

        if (courierId is int id)
            GetCourier(id).Release(parcel);

        Log($"Посылка #{parcel.Id} отменена");
    }

    public IEnumerable<ExpressParcel> OverdueExpress(DateTime now) =>
        _parcels.OfType<ExpressParcel>().Where(p => p.IsOverdue(now));

    public string BuildReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Отчёт по доставке ===");
        sb.AppendLine($"Всего посылок: {_parcels.Count}, курьеров: {_couriers.Count}");
        sb.AppendLine("По статусам:");

        foreach (var group in _parcels.GroupBy(p => p.Status))
            sb.AppendLine($"  {group.Key.ToRussian()}: {group.Count()}");

        sb.AppendLine($"Выручка за доставленные: {Revenue:F2} сомони");
        sb.AppendLine("Загрузка курьеров:");

        foreach (var courier in _couriers)
            sb.AppendLine($"  {courier.Name}: {courier.CurrentLoadKg:F1} из {courier.MaxLoadKg:F1} кг");

        return sb.ToString();
    }

    private void Log(string message) =>
        _journal.Add($"{DateTime.Now:dd.MM.yyyy HH:mm} | {message}");
}
