using AutoService.Domain.Exceptions;

namespace AutoService.Domain.Common;

/// <summary>Проверки входных данных, общие для всех классов модели.</summary>
internal static class Guard
{
    public static string NotEmpty(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"Поле «{fieldName}» обязательно для заполнения.");
        return value.Trim();
    }

    public static decimal Positive(decimal value, string fieldName)
    {
        if (value <= 0)
            throw new DomainException($"Значение «{fieldName}» должно быть больше нуля.");
        return value;
    }

    public static int Positive(int value, string fieldName)
    {
        if (value <= 0)
            throw new DomainException($"Значение «{fieldName}» должно быть больше нуля.");
        return value;
    }
}
