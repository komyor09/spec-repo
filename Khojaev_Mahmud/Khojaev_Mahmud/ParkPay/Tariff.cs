using System;

namespace PaidParking;

// Тариф «Стандартный» из исследования предметной области
public static class Tariff
{
    public const decimal PricePerHour = 60m;   // руб. за час
    public const int FreeMinutes = 15;         // бесплатные минуты
    public const decimal MaxPerDay = 500m;     // максимум за сутки

    // Расчёт стоимости: бесплатные минуты вычитаются, остаток округляется вверх до часа,
    // за каждые полные сутки действует максимум.
    public static decimal Calculate(DateTime entry, DateTime exit)
    {
        if (exit <= entry)
            throw new ArgumentException("Время выезда должно быть позже времени въезда.");

        double billableMinutes = (exit - entry).TotalMinutes - FreeMinutes;
        if (billableMinutes <= 0)
            return 0m;

        int hours = (int)Math.Ceiling(billableMinutes / 60.0);
        int fullDays = hours / 24;
        int restHours = hours % 24;

        decimal rest = Math.Min(restHours * PricePerHour, MaxPerDay);
        return fullDays * MaxPerDay + rest;
    }
}
