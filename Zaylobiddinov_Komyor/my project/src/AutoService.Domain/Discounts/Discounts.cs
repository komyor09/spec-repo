namespace AutoService.Domain.Discounts;

/// <summary>Без скидки.</summary>
public sealed class NoDiscount : IDiscountStrategy
{
    public string Name => "Без скидки";
    public decimal GetPercent() => 0;
}

/// <summary>Произвольная скидка, назначенная мастером.</summary>
public sealed class PercentDiscount : IDiscountStrategy
{
    private readonly decimal _percent;
    public PercentDiscount(decimal percent) => _percent = percent;
    public string Name => $"Скидка {_percent} %";
    public decimal GetPercent() => _percent;
}

/// <summary>Скидка постоянному клиенту: зависит от числа завершённых заявок.</summary>
public sealed class LoyalClientDiscount : IDiscountStrategy
{
    private readonly int _completedVisits;
    public LoyalClientDiscount(int completedVisits) => _completedVisits = completedVisits;
    public string Name => "Постоянный клиент";

    public decimal GetPercent() => _completedVisits switch
    {
        >= 10 => 10,
        >= 5 => 7,
        >= 3 => 5,
        _ => 0
    };
}
