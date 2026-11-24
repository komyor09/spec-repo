using AutoService.Domain.Enums;
using AutoService.Domain.Exceptions;

namespace AutoService.Domain.Entities;

/// <summary>Механик — выполняет работы по своей специализации.</summary>
public class Mechanic : Employee
{
    public Specialization Specialization { get; private set; }

    /// <summary>Разряд (квалификация) от 1 до 6.</summary>
    public int Grade { get; private set; }

    public override string Position => $"Механик ({Specialization.ToRussian()})";

    protected Mechanic() { }

    public Mechanic(string fullName, string phone, DateTime hireDate, Specialization specialization, int grade)
        : base(fullName, phone, hireDate)
    {
        if (grade is < 1 or > 6)
            throw new DomainException("Разряд механика должен быть от 1 до 6.");
        Specialization = specialization;
        Grade = grade;
    }

    /// <summary>Может ли механик выполнить услугу: совпадает специализация или он универсал.</summary>
    public bool CanPerform(Service service) =>
        Specialization == Specialization.Universal || Specialization == service.Specialization;
}
