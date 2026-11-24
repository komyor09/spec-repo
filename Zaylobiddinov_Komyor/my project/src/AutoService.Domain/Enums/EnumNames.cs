namespace AutoService.Domain.Enums;

/// <summary>Русские названия значений перечислений для вывода в интерфейсе.</summary>
public static class EnumNames
{
    public static string ToRussian(this RequestStatus status) => status switch
    {
        RequestStatus.New => "Новая",
        RequestStatus.Diagnostics => "Диагностика",
        RequestStatus.AwaitingApproval => "Ожидает согласования",
        RequestStatus.Approved => "Согласована",
        RequestStatus.InProgress => "В работе",
        RequestStatus.WaitingForParts => "Ожидает запчасти",
        RequestStatus.Ready => "Готова",
        RequestStatus.Issued => "Выдана",
        RequestStatus.Cancelled => "Отменена",
        _ => status.ToString()
    };

    public static string ToRussian(this Specialization specialization) => specialization switch
    {
        Specialization.Universal => "Универсал",
        Specialization.Locksmith => "Слесарь",
        Specialization.Electrician => "Электрик",
        Specialization.BodyWork => "Кузовщик",
        Specialization.Diagnostics => "Диагност",
        _ => specialization.ToString()
    };
}
