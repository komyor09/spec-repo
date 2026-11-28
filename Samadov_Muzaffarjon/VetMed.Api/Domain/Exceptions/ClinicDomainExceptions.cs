namespace VetMed.Api.Domain.Exceptions;

/// <summary>
/// Базовое доменное исключение ветеринарной клиники (иерархия ошибок ООП).
/// </summary>
public abstract class ClinicDomainException : Exception
{
    public int StatusCode { get; }

    protected ClinicDomainException(string message, int statusCode)
        : base(message)
    {
        StatusCode = statusCode;
    }
}

public sealed class DomainValidationException : ClinicDomainException
{
    public DomainValidationException(string message)
        : base(message, StatusCodes.Status400BadRequest)
    {
    }
}

public sealed class EntityNotFoundException : ClinicDomainException
{
    public EntityNotFoundException(string message)
        : base(message, StatusCodes.Status404NotFound)
    {
    }
}

public sealed class ScheduleConflictException : ClinicDomainException
{
    public ScheduleConflictException(string message)
        : base(message, StatusCodes.Status409Conflict)
    {
    }
}
