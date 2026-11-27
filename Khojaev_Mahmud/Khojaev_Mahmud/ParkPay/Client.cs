using System;
using System.Collections.Generic;
using System.Text;

namespace PaidParking;

// Класс 3. Клиент парковки
public class Client
{
    // Задание со звёздочкой: статический счётчик для автоматической генерации Id
    private static int nextId = 1;

    private readonly List<ParkingSession> sessions = new List<ParkingSession>();

    public int Id { get; }
    public string FullName { get; }
    public string Phone { get; }

    public IReadOnlyList<ParkingSession> Sessions => sessions;

    // Общий остаток к оплате по всем сессиям клиента
    public decimal TotalBalance
    {
        get
        {
            decimal total = 0;
            foreach (ParkingSession session in sessions)
                total += session.Balance;
            return total;
        }
    }

    // Обязательные данные: ФИО и телефон (Id назначается автоматически)
    public Client(string fullName, string phone)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("ФИО клиента обязательно.");
        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Телефон клиента обязателен.");

        Id = nextId++;
        FullName = fullName;
        Phone = phone;
    }

    public void AddSession(ParkingSession session)
    {
        if (session == null)
            throw new ArgumentNullException(nameof(session));
        sessions.Add(session);
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Клиент №{Id}: {FullName}, тел. {Phone}");

        foreach (ParkingSession session in sessions)
        {
            sb.AppendLine("  " + session);
            foreach (Payment payment in session.Payments)
                sb.AppendLine("    - платёж: " + payment);
        }

        sb.AppendLine($"Общий остаток к оплате: {TotalBalance:N2} руб.");
        return sb.ToString();
    }
}
