namespace AcademyVector.Api.Domain.Exceptions;

/// <summary>
/// Базовый абстрактный класс иерархии доменных исключений.
/// </summary>
public abstract class DomainException : Exception
{
    public abstract int StatusCode { get; }
    public string ErrorCode { get; }

    protected DomainException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }
}

/// <summary>
/// Ошибка валидации входных данных или нарушения инварианта (HTTP 400).
/// </summary>
public sealed class ValidationDomainException : DomainException
{
    public override int StatusCode => StatusCodes.Status400BadRequest;

    public ValidationDomainException(string message, string errorCode = "VALIDATION_ERROR")
        : base(message, errorCode)
    {
    }
}

/// <summary>
/// Сущность не найдена (HTTP 404).
/// </summary>
public sealed class NotFoundDomainException : DomainException
{
    public override int StatusCode => StatusCodes.Status404NotFound;

    public NotFoundDomainException(string message, string errorCode = "NOT_FOUND")
        : base(message, errorCode)
    {
    }
}

/// <summary>
/// Конфликт состояния или пересечение расписания (HTTP 409).
/// </summary>
public sealed class ConflictDomainException : DomainException
{
    public override int StatusCode => StatusCodes.Status409Conflict;
    public object? Details { get; }

    public ConflictDomainException(string message, string errorCode = "CONFLICT", object? details = null)
        : base(message, errorCode)
    {
        Details = details;
    }
}
