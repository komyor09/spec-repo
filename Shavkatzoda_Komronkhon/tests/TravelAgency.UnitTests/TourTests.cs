using TravelAgency.Domain.Entities;
using Xunit;
using FluentAssertions;

namespace TravelAgency.UnitTests;

public class TourTests
{
    [Fact]
    public void ReserveSeats_WhenRequestedSeatsExceedAvailable_ShouldReturnFailure()
    {
        var tour = new Tour(
            title: "Dubai Safari",
            destination: "UAE",
            price: 500m,
            availableSeats: 3,
            startDate: DateTime.UtcNow.AddDays(5),
            endDate: DateTime.UtcNow.AddDays(10)
        );

        var result = tour.ReserveSeats(5);
        result.IsSuccess.Should().BeFalse();
        tour.AvailableSeats.Should().Be(3);
    }

    [Fact]
    public void ReserveSeats_WhenValidSeatsProvided_ShouldDecreaseAvailableSeatsAndReturnSuccess()
    {
        var tour = new Tour(
            title: "Dubai Safari",
            destination: "UAE",
            price: 500m,
            availableSeats: 5,
            startDate: DateTime.UtcNow.AddDays(5),
            endDate: DateTime.UtcNow.AddDays(10)
        );
        var result = tour.ReserveSeats(2);
        result.IsSuccess.Should().BeTrue();
        tour.AvailableSeats.Should().Be(3);
    }
}