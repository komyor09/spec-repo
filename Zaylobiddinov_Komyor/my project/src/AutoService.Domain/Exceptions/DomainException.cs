namespace AutoService.Domain.Exceptions;

/// <summary>
/// Базовое исключение для нарушений бизнес-правил.
/// Интерфейс перехватывает его и показывает пользователю текст сообщения.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
