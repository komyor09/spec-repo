using AutoService.Domain.Common;

namespace AutoService.Domain.Entities;

/// <summary>
/// Абстрактный класс «Человек»: общие данные клиента и сотрудника.
/// Создать объект Person напрямую нельзя — только его наследников.
/// </summary>
public abstract class Person : Entity
{
    public string FullName { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;

    protected Person() { } // для EF Core

    protected Person(string fullName, string phone)
    {
        Rename(fullName);
        ChangePhone(phone);
    }

    public void Rename(string fullName) => FullName = Guard.NotEmpty(fullName, "ФИО");

    public void ChangePhone(string phone) => Phone = Guard.NotEmpty(phone, "Телефон");

    public override string ToString() => FullName;
}
