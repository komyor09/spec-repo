using System.Security.Cryptography;
using System.Text;
using PrintShop.Core.Data;
using PrintShop.Core.Models;

namespace PrintShop.Core.Services;

/// <summary>Разделы приложения (для разграничения доступа по ролям).</summary>
public enum AppSection
{
    Orders,
    Customers,
    Products,
    Materials,
    Reports,
    Users
}

/// <summary>Действия, на которые нужны права.</summary>
public enum AppPermission
{
    EditOrders,        // создание/редактирование/отмена заказов
    ChangeStatus,      // смена статуса заказа
    AcceptPayments,    // приём оплаты
    EditCustomers,
    EditReferences,    // продукция, услуги, материалы, цены
    ManageUsers,
    Backup
}

/// <summary>Авторизация пользователей и проверка прав доступа.</summary>
public class AuthService
{
    private const string Salt = "PrintShop#2026";

    private static readonly Dictionary<UserRole, AppSection[]> Sections = new()
    {
        [UserRole.Admin] = Enum.GetValues<AppSection>(),
        [UserRole.Manager] = new[] { AppSection.Orders, AppSection.Customers, AppSection.Products, AppSection.Materials, AppSection.Reports },
        [UserRole.Printer] = new[] { AppSection.Orders, AppSection.Materials },
    };

    private static readonly Dictionary<UserRole, AppPermission[]> Permissions = new()
    {
        [UserRole.Admin] = Enum.GetValues<AppPermission>(),
        [UserRole.Manager] = new[] { AppPermission.EditOrders, AppPermission.ChangeStatus, AppPermission.AcceptPayments, AppPermission.EditCustomers },
        [UserRole.Printer] = new[] { AppPermission.ChangeStatus },
    };

    private readonly UserRepository _users;

    public AuthService(UserRepository users) => _users = users;

    /// <summary>Текущий вошедший пользователь.</summary>
    public User? CurrentUser { get; private set; }

    public event EventHandler? UserChanged;

    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(Salt + password));
        return Convert.ToHexString(bytes);
    }

    public static bool VerifyPassword(string password, string hash) =>
        string.Equals(HashPassword(password), hash, StringComparison.OrdinalIgnoreCase);

    /// <summary>Вход в систему. Возвращает пользователя или выбрасывает исключение с понятным сообщением.</summary>
    public User Login(string login, string password)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrEmpty(password))
            throw new OrderValidationException("Введите логин и пароль.");

        var user = _users.GetByLogin(login);
        if (user == null || !VerifyPassword(password, user.PasswordHash))
            throw new OrderValidationException("Неверный логин или пароль.");

        CurrentUser = user;
        UserChanged?.Invoke(this, EventArgs.Empty);
        return user;
    }

    public void Logout()
    {
        CurrentUser = null;
        UserChanged?.Invoke(this, EventArgs.Empty);
    }

    public static bool CanAccess(UserRole role, AppSection section) => Sections[role].Contains(section);
    public static bool Has(UserRole role, AppPermission permission) => Permissions[role].Contains(permission);

    public bool CanAccess(AppSection section) => CurrentUser != null && CanAccess(CurrentUser.Role, section);
    public bool Has(AppPermission permission) => CurrentUser != null && Has(CurrentUser.Role, permission);

    /// <summary>Проверка права с выбросом исключения.</summary>
    public void Demand(AppPermission permission)
    {
        if (!Has(permission))
            throw new OrderValidationException("Недостаточно прав для выполнения операции.");
    }

    // -------- Управление пользователями (для администратора) --------

    public void SaveUser(User user, string? newPassword)
    {
        Demand(AppPermission.ManageUsers);
        if (!string.IsNullOrEmpty(newPassword))
        {
            if (newPassword.Length < 4)
                throw new OrderValidationException("Пароль должен содержать не менее 4 символов.");
            user.PasswordHash = HashPassword(newPassword);
        }
        if (string.IsNullOrEmpty(user.PasswordHash))
            throw new OrderValidationException("Укажите пароль для нового пользователя.");

        var sameLogin = _users.GetByLogin(user.Login);
        if (sameLogin != null && sameLogin.Id != user.Id)
            throw new OrderValidationException($"Пользователь с логином «{user.Login}» уже существует.");

        if (!user.IsNew)
        {
            var old = _users.GetById(user.Id);
            if (old?.Role == UserRole.Admin && user.Role != UserRole.Admin && _users.CountAdmins() <= 1)
                throw new OrderValidationException("Нельзя лишить прав последнего администратора.");
        }

        if (user.IsNew) _users.Add(user); else _users.Update(user);
    }

    public void DeleteUser(User user)
    {
        Demand(AppPermission.ManageUsers);
        if (CurrentUser?.Id == user.Id)
            throw new OrderValidationException("Нельзя удалить пользователя, под которым выполнен вход.");
        if (user.Role == UserRole.Admin && _users.CountAdmins() <= 1)
            throw new OrderValidationException("Нельзя удалить последнего администратора.");
        _users.Delete(user.Id);
    }
}
