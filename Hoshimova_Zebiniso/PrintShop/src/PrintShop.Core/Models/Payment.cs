namespace PrintShop.Core.Models;

/// <summary>Оплата по заказу.</summary>
public class Payment : EntityBase
{
    private decimal _amount;

    public int OrderId { get; set; }

    public decimal Amount
    {
        get => _amount;
        set
        {
            if (value <= 0)
                throw new OrderValidationException("Сумма оплаты должна быть больше нуля.", nameof(Amount));
            _amount = value;
        }
    }

    public DateTime PaymentDate { get; set; } = DateTime.Now;

    public PaymentType PaymentType { get; set; }

    public string PaymentTypeName => EnumNames.Of(PaymentType);
}

/// <summary>Запись истории смены статуса заказа.</summary>
public class StatusHistoryEntry : EntityBase
{
    public int OrderId { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.Now;
    public int? UserId { get; set; }
    public string UserName { get; set; } = string.Empty;

    public string StatusName => EnumNames.Of(Status);
}
