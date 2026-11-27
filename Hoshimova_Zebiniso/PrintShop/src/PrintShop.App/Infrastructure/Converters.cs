using System.Globalization;
using System.Windows;
using System.Windows.Data;
using PrintShop.Core.Models;

namespace PrintShop.App.Infrastructure;

/// <summary>Отображает значение перечисления по-русски.</summary>
public class EnumNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
    {
        OrderStatus s => EnumNames.Of(s),
        UserRole r => EnumNames.Of(r),
        CustomerType t => EnumNames.Of(t),
        ProductKind k => EnumNames.Of(k),
        ColorMode c => EnumNames.Of(c),
        PaymentType p => EnumNames.Of(p),
        ServiceChargeType ch => EnumNames.Of(ch),
        null => string.Empty,
        _ => value.ToString() ?? string.Empty
    };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>true → Visible, false → Collapsed (с параметром "Invert" — наоборот).</summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool v = value is bool b && b;
        if (parameter as string == "Invert") v = !v;
        return v ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
