using AutoService.Domain.Exceptions;

namespace AutoService.Domain.Entities;

/// <summary>Оплата банковской картой: хранит только последние 4 цифры карты.</summary>
public class CardPayment : Payment
{
    public string CardLast4 { get; private set; } = string.Empty;

    public override string Method => "Банковская карта";

    protected CardPayment() { }

    public CardPayment(decimal amount, string cardLast4) : base(amount)
    {
        CardLast4 = cardLast4?.Trim() ?? string.Empty;
    }

    public override void Process()
    {
        if (CardLast4.Length != 4 || !CardLast4.All(char.IsDigit))
            throw new DomainException("Укажите последние 4 цифры карты.");
    }

    public override string GetReceiptLine() => base.GetReceiptLine() + $" (карта **** {CardLast4})";
}
