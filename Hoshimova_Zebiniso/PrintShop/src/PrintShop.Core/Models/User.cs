namespace PrintShop.Core.Models;

/// <summary>Пользователь системы.</summary>
public class User : EntityBase
{
    private string _login = string.Empty;
    private string _fullName = string.Empty;
    private string _passwordHash = string.Empty;

    public string Login
    {
        get => _login;
        set
        {
            var v = RequireText(value, "Логин", 50);
            if (v.Contains(' '))
                throw new OrderValidationException("Логин не должен содержать пробелов.", nameof(Login));
            _login = v.ToLowerInvariant();
        }
    }

    /// <summary>Хэш пароля (SHA-256 с солью). Сам пароль нигде не хранится.</summary>
    public string PasswordHash
    {
        get => _passwordHash;
        set => _passwordHash = value ?? string.Empty;
    }

    public string FullName
    {
        get => _fullName;
        set => _fullName = RequireText(value, "ФИО", 150);
    }

    public UserRole Role { get; set; }

    public string RoleName => EnumNames.Of(Role);

    public override string ToString() => $"{FullName} ({RoleName})";
}
