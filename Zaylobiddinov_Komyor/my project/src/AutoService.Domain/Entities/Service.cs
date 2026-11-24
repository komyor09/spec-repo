using AutoService.Domain.Common;
using AutoService.Domain.Enums;

namespace AutoService.Domain.Entities;

/// <summary>Услуга из прайс-листа. Цена = нормо-часы × стоимость нормо-часа.</summary>
public class Service : Entity
{
    public string Name { get; private set; } = string.Empty;
    public decimal NormHours { get; private set; }
    public decimal HourRate { get; private set; }
    public Specialization Specialization { get; private set; }

    /// <summary>Вычисляемая цена — не хранится, а рассчитывается.</summary>
    public decimal Price => Math.Round(NormHours * HourRate, 2);

    protected Service() { }

    public Service(string name, decimal normHours, decimal hourRate, Specialization specialization)
    {
        Name = Guard.NotEmpty(name, "Наименование");
        Specialization = specialization;
        ChangeTariff(normHours, hourRate);
    }

    public void ChangeTariff(decimal normHours, decimal hourRate)
    {
        NormHours = Guard.Positive(normHours, "Нормо-часы");
        HourRate = Guard.Positive(hourRate, "Стоимость нормо-часа");
    }

    public override string ToString() => $"{Name} — {Price:N2}";
}
