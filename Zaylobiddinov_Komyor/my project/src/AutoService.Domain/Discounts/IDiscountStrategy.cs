namespace AutoService.Domain.Discounts;

/// <summary>Стратегия скидки (паттерн «Стратегия»): каждый класс по-своему определяет процент.</summary>
public interface IDiscountStrategy
{
    string Name { get; }
    decimal GetPercent();
}
