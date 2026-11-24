namespace AutoService.Domain.Enums;

/// <summary>Статусы заявки на ремонт (см. таблицу 1.2 и рисунок 2.3).</summary>
public enum RequestStatus
{
    New,
    Diagnostics,
    AwaitingApproval,
    Approved,
    InProgress,
    WaitingForParts,
    Ready,
    Issued,
    Cancelled
}
