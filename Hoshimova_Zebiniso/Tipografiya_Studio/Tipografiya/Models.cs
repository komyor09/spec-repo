using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Tipografiya;

public static class Display
{
    public static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");
    public static string Money(decimal value) => value.ToString("N2", Ru);
    public static string Digits(string value) => new(value.Where(char.IsDigit).ToArray());
}

public sealed class Client
{
    public int Id { get; init; }
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public override string ToString() => $"{Name} · {Phone}";
}

public enum PrintProduct { Service, Flyer, Brochure }

// A position holds the immutable tariff agreed when the order was entered.
public sealed record OrderLine(Tariff Tariff, decimal Quantity, PrintProduct Product = PrintProduct.Service,
    int Units = 1, decimal Binding = 0m)
{
    public decimal Amount => decimal.Round(Quantity * (Units * Tariff.Rate + Binding), 2,
        MidpointRounding.AwayFromZero);
    public string QuantityText => Quantity.ToString("0.###", Display.Ru);
    public string Description => Product switch
    {
        PrintProduct.Brochure => $"Брошюра · {Tariff.Name} · {Units} стр.",
        PrintProduct.Flyer => $"Листовка · {Tariff.Name} · {Units} стор.",
        _ => $"{Tariff.Name} ({Tariff.UnitText})"
    };
    public string Formula => Product switch
    {
        PrintProduct.Brochure => $"{QuantityText} × ({Units} × {Tariff.Price} + {Display.Money(Binding)})",
        PrintProduct.Flyer => $"{QuantityText} × {Units} × {Tariff.Price}",
        _ => $"{QuantityText} × {Tariff.Price}"
    };
    public string AmountText => Display.Money(Amount);
}

public sealed record HistoryEntry(DateTime Time, string Status, string Reason)
{
    public override string ToString() =>
        $"{Time:dd.MM.yyyy HH:mm}   {Status}" + (Reason.Length > 0 ? $" · {Reason}" : "");
}

public sealed class PrintOrder
{
    public int Id { get; init; }
    public DateTime Created { get; init; } = DateTime.Today;
    public required Client Customer { get; set; }
    public DateTime Due { get; set; }
    public string Comment { get; set; } = "";
    public string Status { get; private set; } = "Принят";
    public List<OrderLine> Lines { get; set; } = new();
    public List<HistoryEntry> History { get; } = new();
    public string CustomerName => Customer.Name;
    public string DueText => Due.ToString("dd.MM.yyyy");
    public decimal Total => Lines.Sum(l => l.Amount);
    public string TotalText => Display.Money(Total);
    public bool Overdue => Due.Date < DateTime.Today && (Status == "Принят" || Status == "В печати");
    public string DueMark => Overdue ? "Просрочен" : "";
    public string? NextStatus => Status switch
    {
        "Принят" => "В печати", "В печати" => "Готов", "Готов" => "Выдан", _ => null
    };
    internal void RestoreHistory(HistoryEntry entry)
    {
        History.Add(entry);
        Status = entry.Status;
    }

    public void Register()
    {
        if (History.Count == 0) History.Add(new(DateTime.Now, Status, ""));
    }
    public void ChangeStatus(string next, string reason = "")
    {
        bool cancel = Status == "Принят" && next == "Отменён" && !string.IsNullOrWhiteSpace(reason);
        if (next != NextStatus && !cancel) throw new InvalidOperationException("Переход недоступен.");
        Status = next;
        History.Add(new(DateTime.Now, next, reason.Trim()));
    }
}
