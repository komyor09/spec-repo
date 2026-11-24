using AutoService.Domain.Discounts;
using AutoService.Domain.Entities;
using AutoService.Domain.Enums;

namespace AutoService.Data;

/// <summary>Начальные данные для демонстрации: прайс, склад, сотрудники, клиенты и несколько заявок.</summary>
public static class DbSeeder
{
    public static void Seed(AppDbContext db)
    {
        db.Database.EnsureCreated();
        if (db.Services.Any())
            return; // база уже заполнена

        // --- справочники ---
        var oilChange = new Service("Замена моторного масла", 0.5m, 150m, Specialization.Locksmith);
        var brakes = new Service("Замена тормозных колодок (ось)", 1.0m, 150m, Specialization.Locksmith);
        var suspension = new Service("Диагностика подвески", 0.8m, 150m, Specialization.Diagnostics);
        var computerDiag = new Service("Компьютерная диагностика", 0.5m, 200m, Specialization.Diagnostics);
        var wiring = new Service("Ремонт электропроводки", 2.0m, 180m, Specialization.Electrician);
        var battery = new Service("Замена аккумулятора", 0.3m, 180m, Specialization.Electrician);
        var painting = new Service("Покраска элемента кузова", 4.0m, 200m, Specialization.BodyWork);
        db.Services.AddRange(oilChange, brakes, suspension, computerDiag, wiring, battery, painting);

        var oil = new SparePart("OIL-5W30-4L", "Масло моторное 5W-30, 4 л", 320m, 20);
        var oilFilter = new SparePart("FLT-OIL-01", "Фильтр масляный", 60m, 25);
        var pads = new SparePart("BRK-PAD-F", "Колодки тормозные передние", 280m, 8);
        var batteryPart = new SparePart("BAT-60AH", "Аккумулятор 60 А·ч", 950m, 3);
        var bulb = new SparePart("LMP-H4", "Лампа H4", 35m, 40);
        db.SpareParts.AddRange(oil, oilFilter, pads, batteryPart, bulb);

        var receptionist = new Receptionist("Саидова Нигина", "+992 92 700 11 22", new DateTime(2024, 3, 1));
        var locksmith = new Mechanic("Рахимов Далер", "+992 93 500 22 33", new DateTime(2022, 6, 15), Specialization.Locksmith, 5);
        var electrician = new Mechanic("Назаров Фаррух", "+992 90 400 33 44", new DateTime(2023, 2, 10), Specialization.Electrician, 4);
        var diagnost = new Mechanic("Юсупов Бахром", "+992 91 300 44 55", new DateTime(2021, 9, 1), Specialization.Diagnostics, 6);
        var universal = new Mechanic("Каримов Шахром", "+992 98 200 55 66", new DateTime(2020, 1, 20), Specialization.Universal, 6);
        db.Employees.AddRange(receptionist, locksmith, electrician, diagnost, universal);

        var client1 = new Client("Алиев Фирдавс", "+992 92 111 22 33", "firdavs@mail.tj");
        var car1 = client1.AddCar("Toyota", "Camry", 2018, "1234 AB 02", "JTNB11HK0J3000001", 145_000);
        var client2 = new Client("Шарипова Мадина", "+992 93 222 33 44");
        var car2 = client2.AddCar("Hyundai", "Elantra", 2020, "5678 AC 02", null, 62_000);
        var client3 = new Client("Холматов Сухроб", "+992 90 333 44 55");
        var car3 = client3.AddCar("Chevrolet", "Cobalt", 2021, "9012 AD 02", null, 38_000);
        db.Clients.AddRange(client1, client2, client3);

        db.SaveChanges(); // сначала сохраняем справочники, чтобы у них появились Id

        // --- заявки в разных статусах ---
        var today = DateTime.Today;

        var issued = new RepairRequest($"{today:yyMMdd}-001", client1, car1, "Плановое ТО: замена масла", receptionist);
        issued.StartDiagnostics();
        issued.AddWork(oilChange, locksmith);
        issued.AddPart(oil, 1);
        issued.AddPart(oilFilter, 1);
        issued.SendForApproval();
        issued.Approve();
        issued.StartWork();
        issued.Complete();
        issued.Pay(new CashPayment(issued.Total, 500m));

        var inProgress = new RepairRequest($"{today:yyMMdd}-002", client2, car2, "Скрип при торможении, не горит левая фара", receptionist);
        inProgress.StartDiagnostics();
        inProgress.AddWork(brakes, locksmith);
        inProgress.AddWork(battery, electrician);
        inProgress.AddPart(pads, 1);
        inProgress.AddPart(bulb, 1);
        inProgress.ApplyDiscount(new PercentDiscount(5));
        inProgress.SendForApproval();
        inProgress.Approve();
        inProgress.StartWork();

        var fresh = new RepairRequest($"{today:yyMMdd}-003", client3, car3, "Стук спереди справа на неровностях", receptionist);

        db.RepairRequests.AddRange(issued, inProgress, fresh);
        db.SaveChanges();
    }
}
