using System;
using System.Windows.Forms;

namespace PaidParking;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Клиент (Id назначается автоматически статическим счётчиком)
        Client client = new Client("Иванов Пётр Сергеевич", "+7 900 123-45-67");

        // Два счёта (парковочные сессии), стоимость считается по тарифу
        DateTime in1 = new DateTime(2026, 10, 17, 9, 10, 0);
        DateTime out1 = new DateTime(2026, 10, 17, 12, 40, 0);
        ParkingSession first = new ParkingSession(1001, "А123ВС77", in1, out1, Tariff.Calculate(in1, out1));   // 240 руб.

        DateTime in2 = new DateTime(2026, 10, 18, 18, 0, 0);
        DateTime out2 = new DateTime(2026, 10, 18, 19, 30, 0);
        ParkingSession second = new ParkingSession(1002, "А123ВС77", in2, out2, Tariff.Calculate(in2, out2)); // 120 руб.

        client.AddSession(first);
        client.AddSession(second);

        // Три операции (платежа)
        first.Pay(240m, "банковская карта", new DateTime(2026, 10, 17, 12, 38, 0));
        second.Pay(50m, "наличные", new DateTime(2026, 10, 18, 19, 20, 0));
        second.Pay(30m, "банковская карта", new DateTime(2026, 10, 18, 19, 25, 0));

        // first.Balance = 0; // ошибка компиляции: set недоступен снаружи

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm(client));
    }
}
