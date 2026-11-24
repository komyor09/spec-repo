using AutoService.Domain.Common;

namespace AutoService.Domain.Entities;

/// <summary>
/// Абстрактная оплата. Каждый способ оплаты по-своему реализует проверку Process()
/// и название Method — заявка работает с базовым типом, не зная конкретного (полиморфизм).
/// </summary>
public abstract class Payment : Entity
{
    public int RepairRequestId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime PaidAt { get; private set; }

    public abstract string Method { get; }

    protected Payment() { }

    protected Payment(decimal amount)
    {
        Amount = Guard.Positive(amount, "Сумма оплаты");
        PaidAt = DateTime.Now;
    }

    /// <summary>Проводит оплату. При ошибке выбрасывает DomainException.</summary>
    public abstract void Process();

    /// <summary>Текст для чека — общий шаблон, детали дополняют наследники.</summary>
    public virtual string GetReceiptLine() => $"{PaidAt:dd.MM.yyyy HH:mm}  {Method}: {Amount:N2}";
}
