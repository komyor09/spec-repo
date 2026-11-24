using AutoService.Domain.Enums;

namespace AutoService.Domain.Interfaces;

/// <summary>Параметры поиска заявок. Пустое поле — без ограничения.</summary>
public record RequestFilter(RequestStatus? Status = null, string? Text = null, bool HideClosed = false);
