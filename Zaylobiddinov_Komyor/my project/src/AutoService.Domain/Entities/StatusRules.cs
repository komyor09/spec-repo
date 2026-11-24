using AutoService.Domain.Enums;

namespace AutoService.Domain.Entities;

/// <summary>
/// Таблица допустимых переходов статусов заявки (таблица 1.2, рисунок 2.3).
/// Правило записано в одном месте — все методы смены статуса обращаются сюда.
/// </summary>
public static class StatusRules
{
    private static readonly Dictionary<RequestStatus, RequestStatus[]> Transitions = new()
    {
        [RequestStatus.New] = [RequestStatus.Diagnostics, RequestStatus.Cancelled],
        [RequestStatus.Diagnostics] = [RequestStatus.AwaitingApproval, RequestStatus.Cancelled],
        [RequestStatus.AwaitingApproval] = [RequestStatus.Approved, RequestStatus.Cancelled],
        [RequestStatus.Approved] = [RequestStatus.InProgress, RequestStatus.Cancelled],
        [RequestStatus.InProgress] = [RequestStatus.WaitingForParts, RequestStatus.Ready],
        [RequestStatus.WaitingForParts] = [RequestStatus.InProgress, RequestStatus.Cancelled],
        [RequestStatus.Ready] = [RequestStatus.Issued],
        [RequestStatus.Issued] = [],
        [RequestStatus.Cancelled] = [],
    };

    public static bool CanMove(RequestStatus from, RequestStatus to) =>
        Transitions.TryGetValue(from, out var next) && next.Contains(to);

    public static IReadOnlyList<RequestStatus> NextStatuses(RequestStatus from) =>
        Transitions.TryGetValue(from, out var next) ? next : [];
}
