using AutoService.Domain.Enums;

namespace AutoService.Domain.Exceptions;

/// <summary>Попытка перевести заявку в статус, недопустимый из текущего.</summary>
public class InvalidStatusTransitionException : DomainException
{
    public RequestStatus From { get; }
    public RequestStatus To { get; }

    public InvalidStatusTransitionException(RequestStatus from, RequestStatus to)
        : base($"Нельзя перевести заявку из статуса «{from.ToRussian()}» в статус «{to.ToRussian()}».")
    {
        From = from;
        To = to;
    }
}
