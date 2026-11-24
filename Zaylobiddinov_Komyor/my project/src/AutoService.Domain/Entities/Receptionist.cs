namespace AutoService.Domain.Entities;

/// <summary>Мастер-приёмщик — принимает автомобили и оформляет заявки.</summary>
public class Receptionist : Employee
{
    public override string Position => "Мастер-приёмщик";

    protected Receptionist() { }

    public Receptionist(string fullName, string phone, DateTime hireDate)
        : base(fullName, phone, hireDate) { }
}
