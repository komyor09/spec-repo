using AutoService.Domain.Entities;
using AutoService.Domain.Enums;

namespace AutoService.Tests;

/// <summary>Готовые объекты для тестов.</summary>
internal static class TestData
{
    public static Client Client() => new("Иванов Иван", "+992900000001");

    public static (Client client, Car car) ClientWithCar()
    {
        var client = Client();
        var car = client.AddCar("Toyota", "Camry", 2018, "1234 AA 01", null, 120_000);
        return (client, car);
    }

    public static Service OilChange() => new("Замена масла", 0.5m, 200m, Specialization.Locksmith);
    public static Service Wiring() => new("Ремонт проводки", 2m, 250m, Specialization.Electrician);

    public static Mechanic Locksmith() =>
        new("Петров Пётр", "+992900000002", DateTime.Today, Specialization.Locksmith, 4);

    public static SparePart Oil(int stock = 10) => new("OIL-5W30", "Масло 5W-30, 4 л", 350m, stock);

    public static RepairRequest NewRequest()
    {
        var (client, car) = ClientWithCar();
        return new RepairRequest("261114-001", client, car, "Пора менять масло");
    }

    /// <summary>Заявка с работой и запчастью, доведённая до статуса «Готова».</summary>
    public static RepairRequest ReadyRequest()
    {
        var r = NewRequest();
        r.StartDiagnostics();
        r.AddWork(OilChange(), Locksmith()); // 100
        r.AddPart(Oil(), 1);                 // 350
        r.SendForApproval();
        r.Approve();
        r.StartWork();
        r.Complete();
        return r;
    }
}
