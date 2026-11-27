namespace PrintShop.Core.Models;

/// <summary>Базовый класс всех сущностей, хранящихся в базе данных.</summary>
public abstract class EntityBase
{
    private int _id;

    public int Id
    {
        get => _id;
        set
        {
            if (value < 0) throw new OrderValidationException("Идентификатор не может быть отрицательным.", nameof(Id));
            _id = value;
        }
    }

    public bool IsNew => _id == 0;

    /// <summary>Общая проверка строковых полей — вспомогательный метод для наследников.</summary>
    protected static string RequireText(string? value, string fieldName, int maxLength = 200)
    {
        var v = value?.Trim() ?? string.Empty;
        if (v.Length == 0)
            throw new OrderValidationException($"Поле «{fieldName}» обязательно для заполнения.", fieldName);
        if (v.Length > maxLength)
            throw new OrderValidationException($"Поле «{fieldName}» не должно превышать {maxLength} символов.", fieldName);
        return v;
    }

    protected static decimal RequireNonNegative(decimal value, string fieldName)
    {
        if (value < 0)
            throw new OrderValidationException($"Значение «{fieldName}» не может быть отрицательным.", fieldName);
        return value;
    }
}
