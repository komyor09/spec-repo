using AutoService.Domain.Discounts;
using AutoService.Domain.Entities;
using AutoService.Domain.Enums;
using AutoService.Domain.Exceptions;

namespace AutoService.Tests;

public class OtherDomainTests
{
    [Fact]
    public void Service_price_is_norm_hours_times_rate()
    {
        var s = new Service("Диагностика ходовой", 1.5m, 300m, Specialization.Diagnostics);

        Assert.Equal(450m, s.Price);
    }

    [Fact]
    public void Service_requires_positive_tariff()
    {
        Assert.Throws<DomainException>(() => new Service("X", 0, 100, Specialization.Locksmith));
    }

    [Fact]
    public void Mileage_cannot_decrease()
    {
        var (_, car) = TestData.ClientWithCar();

        Assert.Throws<DomainException>(() => car.UpdateMileage(100));
        car.UpdateMileage(130_000);
        Assert.Equal(130_000, car.Mileage);
    }

    [Fact]
    public void Plate_number_is_normalized()
    {
        var (_, car) = TestData.ClientWithCar();

        Assert.Equal("1234AA01", car.PlateNumber);
    }

    [Fact]
    public void Employee_position_is_polymorphic()
    {
        List<Employee> staff =
        [
            new Mechanic("А", "1", DateTime.Today, Specialization.Electrician, 3),
            new Receptionist("Б", "2", DateTime.Today)
        ];

        var positions = staff.Select(e => e.Position).ToList();

        Assert.Equal(new[] { "Механик (Электрик)", "Мастер-приёмщик" }, positions);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(2, 0)]
    [InlineData(3, 5)]
    [InlineData(5, 7)]
    [InlineData(12, 10)]
    public void Loyal_client_discount_depends_on_visits(int visits, int expectedPercent)
    {
        IDiscountStrategy strategy = new LoyalClientDiscount(visits);

        Assert.Equal(expectedPercent, strategy.GetPercent());
    }

    [Fact]
    public void Status_rules_match_table()
    {
        Assert.True(StatusRules.CanMove(RequestStatus.New, RequestStatus.Diagnostics));
        Assert.False(StatusRules.CanMove(RequestStatus.New, RequestStatus.Issued));
        Assert.False(StatusRules.CanMove(RequestStatus.InProgress, RequestStatus.Cancelled));
        Assert.Empty(StatusRules.NextStatuses(RequestStatus.Issued));
    }
}
