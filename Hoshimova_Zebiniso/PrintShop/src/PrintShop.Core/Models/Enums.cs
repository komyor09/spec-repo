namespace PrintShop.Core.Models;

/// <summary>Статусы заказа (жизненный цикл заказа в типографии).</summary>
public enum OrderStatus
{
    New = 0,            // Новый
    LayoutApproval = 1, // Согласование макета
    Printing = 2,       // В печати
    PostProcessing = 3, // Постобработка
    Ready = 4,          // Готов
    Issued = 5,         // Выдан
    Cancelled = 6       // Отменён
}

/// <summary>Роли пользователей системы.</summary>
public enum UserRole
{
    Admin = 0,   // Администратор
    Manager = 1, // Менеджер
    Printer = 2  // Печатник
}

/// <summary>Тип клиента.</summary>
public enum CustomerType
{
    Person = 0,  // Физическое лицо
    Company = 1  // Юридическое лицо
}

/// <summary>Вид печатной продукции (определяет класс-наследник PrintProduct).</summary>
public enum ProductKind
{
    BusinessCard = 0, // Визитки
    Flyer = 1,        // Листовки
    Booklet = 2,      // Буклеты
    Poster = 3,       // Плакаты
    Banner = 4,       // Баннеры
    Sticker = 5       // Наклейки
}

/// <summary>Цветность печати.</summary>
public enum ColorMode
{
    BlackWhite = 0,   // Ч/б (1+0)
    ColorOneSide = 1, // Цветная односторонняя (4+0)
    ColorTwoSide = 2  // Цветная двусторонняя (4+4)
}

/// <summary>Тип оплаты.</summary>
public enum PaymentType
{
    Prepayment = 0, // Предоплата
    Final = 1       // Окончательная оплата
}

/// <summary>Способ начисления стоимости дополнительной услуги.</summary>
public enum ServiceChargeType
{
    PerUnit = 0,  // за единицу тиража
    PerItem = 1   // фиксированно за позицию заказа
}

/// <summary>Состояние срока заказа (для подсветки в таблице).</summary>
public enum DueState
{
    Normal,
    Today,
    Overdue,
    Closed
}
