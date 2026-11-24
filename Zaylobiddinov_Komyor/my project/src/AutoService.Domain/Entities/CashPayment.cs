using AutoService.Domain.Exceptions;

namespace AutoService.Domain.Entities;

/// <summary>Оплата наличными: проверяет, что внесённой суммы достаточно, и считает сдачу.</summary>
public class CashPayment : Payment
{
    public decimal Received { get; private set; }
    public decimal Change => Received - Amount;

    public override string Method => "Наличные";

    protected CashPayment() { }

    public CashPayment(decimal amount, decimal received) : base(amount)
    {
        Received = received;
    }

    public override void Process()
    {
        if (Received < Amount)
            throw new DomainException($"Внесено {Received:N2}, а к оплате {Amount:N2}.");
    }

    public override string GetReceiptLine() => base.GetReceiptLine() + $" (внесено {Received:N2}, сдача {Change:N2})";
}
