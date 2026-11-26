using System;

namespace PaidParking;

// Класс 1. Платёж — операция по парковочной сессии
public class Payment
{
    public DateTime PaidAt { get; }
    public decimal Amount { get; }
    public string Method { get; }

    // Обязательные данные: сумма, способ оплаты и время платежа
    public Payment(decimal amount, string method, DateTime paidAt)
    {
        if (amount <= 0)
            throw new ArgumentException("Сумма платежа должна быть больше нуля.");
        if (string.IsNullOrWhiteSpace(method))
            throw new ArgumentException("Способ оплаты обязателен.");

        Amount = amount;
        Method = method;
        PaidAt = paidAt;
    }

    public override string ToString()
    {
        return $"{PaidAt:dd.MM.yyyy HH:mm} | {Amount:N2} руб. | {Method}";
    }
}
