namespace ShopApp.Infrastructure;

/// <summary>
/// Ожидаемая ошибка бизнес-логики (нет товара, пустая корзина и т.п.).
/// Middleware превращает её в JSON-ответ { "error": "..." } с нужным кодом.
/// </summary>
public class ShopException(string message, int statusCode = StatusCodes.Status400BadRequest)
    : Exception(message)
{
    public int StatusCode { get; } = statusCode;

    public static ShopException BadRequest(string message) => new(message, StatusCodes.Status400BadRequest);
    public static ShopException NotFound(string message) => new(message, StatusCodes.Status404NotFound);
}
