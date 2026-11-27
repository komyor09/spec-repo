using System.Text.RegularExpressions;

namespace PrintShop.Core.Models;

/// <summary>
/// Клиент типографии. Абстрактный базовый класс для физических и юридических лиц.
/// </summary>
public abstract class Customer : EntityBase
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private static readonly Regex PhoneRegex = new(@"^\+?[0-9\s\-\(\)]{5,20}$", RegexOptions.Compiled);

    private string _name = string.Empty;
    private string _phone = string.Empty;
    private string _email = string.Empty;
    private string _address = string.Empty;

    /// <summary>ФИО (для физ. лица) или название организации (для юр. лица).</summary>
    public string Name
    {
        get => _name;
        set => _name = RequireText(value, NameCaption, 200);
    }

    public string Phone
    {
        get => _phone;
        set
        {
            var v = RequireText(value, "Телефон", 20);
            if (!PhoneRegex.IsMatch(v))
                throw new OrderValidationException("Телефон указан в неверном формате (пример: +992 92 123 45 67).", nameof(Phone));
            _phone = v;
        }
    }

    public string Email
    {
        get => _email;
        set
        {
            var v = value?.Trim() ?? string.Empty;
            if (v.Length > 0 && !EmailRegex.IsMatch(v))
                throw new OrderValidationException("E-mail указан в неверном формате.", nameof(Email));
            _email = v;
        }
    }

    public string Address
    {
        get => _address;
        set => _address = value?.Trim() ?? string.Empty;
    }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>Тип клиента — определяется конкретным наследником.</summary>
    public abstract CustomerType Type { get; }

    /// <summary>Подпись поля «Имя» — у каждого наследника своя.</summary>
    public abstract string NameCaption { get; }

    public string TypeName => EnumNames.Of(Type);

    /// <summary>Строка для отображения в документах (квитанциях).</summary>
    public virtual string GetDisplayName() => Name;

    public override string ToString() => $"{Name}, {Phone}";

    /// <summary>Фабричный метод: создаёт клиента нужного типа.</summary>
    public static Customer Create(CustomerType type) => type switch
    {
        CustomerType.Company => new CompanyCustomer(),
        _ => new PersonCustomer()
    };
}

/// <summary>Физическое лицо.</summary>
public class PersonCustomer : Customer
{
    public override CustomerType Type => CustomerType.Person;
    public override string NameCaption => "ФИО";
}

/// <summary>Юридическое лицо (организация).</summary>
public class CompanyCustomer : Customer
{
    public override CustomerType Type => CustomerType.Company;
    public override string NameCaption => "Название организации";
    public override string GetDisplayName() => $"Организация «{Name}»";
}
