namespace PrintShop.Core.Models;

/// <summary>Русские названия значений перечислений для отображения в интерфейсе.</summary>
public static class EnumNames
{
    public static string Of(OrderStatus s) => s switch
    {
        OrderStatus.New => "Новый",
        OrderStatus.LayoutApproval => "Согласование макета",
        OrderStatus.Printing => "В печати",
        OrderStatus.PostProcessing => "Постобработка",
        OrderStatus.Ready => "Готов",
        OrderStatus.Issued => "Выдан",
        OrderStatus.Cancelled => "Отменён",
        _ => s.ToString()
    };

    public static string Of(UserRole r) => r switch
    {
        UserRole.Admin => "Администратор",
        UserRole.Manager => "Менеджер",
        UserRole.Printer => "Печатник",
        _ => r.ToString()
    };

    public static string Of(CustomerType t) => t switch
    {
        CustomerType.Person => "Физ. лицо",
        CustomerType.Company => "Юр. лицо",
        _ => t.ToString()
    };

    public static string Of(ProductKind k) => k switch
    {
        ProductKind.BusinessCard => "Визитки",
        ProductKind.Flyer => "Листовки",
        ProductKind.Booklet => "Буклеты",
        ProductKind.Poster => "Плакаты",
        ProductKind.Banner => "Баннеры",
        ProductKind.Sticker => "Наклейки",
        _ => k.ToString()
    };

    public static string Of(ColorMode c) => c switch
    {
        ColorMode.BlackWhite => "Ч/б (1+0)",
        ColorMode.ColorOneSide => "Цветная (4+0)",
        ColorMode.ColorTwoSide => "Цветная двусторонняя (4+4)",
        _ => c.ToString()
    };

    public static string Of(PaymentType p) => p switch
    {
        PaymentType.Prepayment => "Предоплата",
        PaymentType.Final => "Окончательная оплата",
        _ => p.ToString()
    };

    public static string Of(ServiceChargeType c) => c switch
    {
        ServiceChargeType.PerUnit => "за единицу тиража",
        ServiceChargeType.PerItem => "за позицию",
        _ => c.ToString()
    };
}
