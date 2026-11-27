using System.Globalization;
using PrintShop.Core.Models;

namespace PrintShop.App.Infrastructure;

/// <summary>Разбор чисел, введённых пользователем (допускаются и запятая, и точка).</summary>
public static class Input
{
    public static decimal Decimal(string? text, string field)
    {
        var t = (text ?? "").Trim().Replace(" ", "").Replace(',', '.');
        if (!decimal.TryParse(t, NumberStyles.Number, CultureInfo.InvariantCulture, out var v))
            throw new OrderValidationException($"Поле «{field}»: введите число.", field);
        return v;
    }

    public static int Int(string? text, string field)
    {
        var t = (text ?? "").Trim().Replace(" ", "");
        if (!int.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
            throw new OrderValidationException($"Поле «{field}»: введите целое число.", field);
        return v;
    }

    public static string Format(decimal v) => v.ToString("0.##", CultureInfo.GetCultureInfo("ru-RU"));
}
