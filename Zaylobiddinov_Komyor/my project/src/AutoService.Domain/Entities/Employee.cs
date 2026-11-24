namespace AutoService.Domain.Entities;

/// <summary>Абстрактный сотрудник автосервиса. Должность определяет каждый наследник.</summary>
public abstract class Employee : Person
{
    public DateTime HireDate { get; private set; }

    /// <summary>Название должности — переопределяется в наследниках (полиморфизм).</summary>
    public abstract string Position { get; }

    protected Employee() { }

    protected Employee(string fullName, string phone, DateTime hireDate) : base(fullName, phone)
    {
        HireDate = hireDate.Date;
    }
}
