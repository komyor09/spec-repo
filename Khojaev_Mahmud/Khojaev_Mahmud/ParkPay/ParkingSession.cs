using System;
using System.Collections.Generic;

namespace PaidParking;

// Класс 2. Парковочная сессия — «счёт» клиента за стоянку
public class ParkingSession
{
    private readonly List<Payment> payments = new List<Payment>();

    public int Id { get; }                  // номер талона
    public string PlateNumber { get; }      // государственный номер автомобиля
    public DateTime EntryTime { get; }
    public DateTime ExitTime { get; }
    public decimal Cost { get; }            // стоимость стоянки по тарифу

    // Остаток к оплате: снаружи только чтение, изменять может только сам класс
    public decimal Balance { get; private set; }

    public string Status => Balance == 0 ? "оплачена" : "не оплачена";

    public IReadOnlyList<Payment> Payments => payments;

    // Обязательные данные: номер сессии, номер автомобиля, время въезда и выезда, стоимость
    public ParkingSession(int id, string plateNumber, DateTime entryTime, DateTime exitTime, decimal cost)
    {
        if (id <= 0)
            throw new ArgumentException("Номер сессии должен быть положительным.");
        if (string.IsNullOrWhiteSpace(plateNumber))
            throw new ArgumentException("Номер автомобиля обязателен.");
        if (exitTime <= entryTime)
            throw new ArgumentException("Время выезда должно быть позже времени въезда.");
        if (cost <= 0)
            throw new ArgumentException("Стоимость должна быть больше нуля.");

        Id = id;
        PlateNumber = plateNumber;
        EntryTime = entryTime;
        ExitTime = exitTime;
        Cost = cost;
        Balance = cost; // сначала клиент должен всю сумму
    }

    // Операция: внести платёж по сессии
    public void Pay(decimal amount, string method, DateTime paidAt)
    {
        if (amount > Balance)
            throw new InvalidOperationException(
                $"Сумма платежа ({amount:N2}) больше остатка к оплате ({Balance:N2}).");

        Payment payment = new Payment(amount, method, paidAt);
        payments.Add(payment);
        Balance -= amount;
    }

    public override string ToString()
    {
        return $"Сессия №{Id}, авто {PlateNumber}, " +
               $"{EntryTime:dd.MM.yyyy HH:mm} – {ExitTime:dd.MM.yyyy HH:mm}, " +
               $"стоимость {Cost:N2} руб., остаток {Balance:N2} руб. ({Status})";
    }
}
