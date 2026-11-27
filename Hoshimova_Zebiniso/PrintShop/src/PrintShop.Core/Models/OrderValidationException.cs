namespace PrintShop.Core.Models;

/// <summary>
/// Собственное исключение для ошибок ввода данных и недопустимых операций
/// (например, отрицательная цена, недопустимый переход статуса, нехватка материала).
/// </summary>
public class OrderValidationException : Exception
{
    public string? PropertyName { get; }

    public OrderValidationException(string message) : base(message) { }

    public OrderValidationException(string message, string propertyName) : base(message)
    {
        PropertyName = propertyName;
    }

    public OrderValidationException(string message, Exception inner) : base(message, inner) { }
}
