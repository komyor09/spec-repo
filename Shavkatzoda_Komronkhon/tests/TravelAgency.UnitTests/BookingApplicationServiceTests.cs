using FluentAssertions;
using NSubstitute;
using System.Diagnostics.Metrics;
using TravelAgency.Application.Bookings;
using TravelAgency.Application.Common;
using TravelAgency.Application.Common.Diagnostics;
using TravelAgency.Application.Interfaces;
using TravelAgency.Domain.Entities;
using Xunit;

namespace TravelAgency.UnitTests;

public class BookingApplicationServiceTests
{
    [Fact]
    public async Task ReserveSeatsAsync_WhenTourNotFound_ShouldReturnNotFoundError()
    {
        var tourRepositoryMock = Substitute.For<ITourRepository>();
        var bookingRepositoryMock = Substitute.For<IBookingRepository>();
        var meterFactoryMock = Substitute.For<IMeterFactory>();
        var eventBusMock = Substitute.For<IEventBus>();

        meterFactoryMock
            .Create(Arg.Any<MeterOptions>())
            .Returns(new Meter("TravelAgency.Tests"));

        var metrics = new TravelAgencyMetrics(meterFactoryMock);

        var nonExistentTourId = Guid.NewGuid();

        tourRepositoryMock
            .GetByIdAsync(nonExistentTourId, Arg.Any<CancellationToken>())
            .Returns((Tour?)null);

        var service = new BookingApplicationService(
            tourRepositoryMock,
            bookingRepositoryMock,
            metrics,
            eventBusMock
        );

        var request = new CreateBookingRequest(
            TourId: nonExistentTourId,
            CustomerName: "Komronkhon",
            CustomerEmail: "koma@example.com",
            SeatsCount: 2
        );

        var result = await service.CreateBookingAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Tour.NotFound");

        await bookingRepositoryMock
            .DidNotReceive()
            .AddAsync(Arg.Any<Booking>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateBookingAsync_WhenTourHasAvailableSeats_ShouldDecreaseSeatsAndSaveBooking()
    {
        var tourRepositoryMock = Substitute.For<ITourRepository>();
        var bookingRepositoryMock = Substitute.For<IBookingRepository>();
        var eventBusMock = Substitute.For<IEventBus>();
        var meterFactoryMock = Substitute.For<IMeterFactory>();
        meterFactoryMock
            .Create(Arg.Any<MeterOptions>())
            .Returns(new Meter("TravelAgency.Tests"));

        var metrics = new TravelAgencyMetrics(meterFactoryMock);

        var existingTour = new Tour(
            title: "Imperial Dubai",
            destination: "UAE",
            price: 1000m,
            availableSeats: 10,
            startDate: DateTime.UtcNow.AddDays(10),
            endDate: DateTime.UtcNow.AddDays(17)
        );

        tourRepositoryMock
            .GetByIdAsync(existingTour.Id, Arg.Any<CancellationToken>())
            .Returns(existingTour);

        var service = new BookingApplicationService(
            tourRepositoryMock,
            bookingRepositoryMock,
            metrics,
            eventBusMock
        );

        var request = new CreateBookingRequest(
            TourId: existingTour.Id,
            CustomerName: "Komronkhon",
            CustomerEmail: "koma@example.com",
            SeatsCount: 2
        );

        var result = await service.CreateBookingAsync(request);

        result.IsSuccess.Should().BeTrue();
        existingTour.AvailableSeats.Should().Be(8);

        await bookingRepositoryMock
            .Received(1)
            .AddAsync(Arg.Is<Booking>(b =>
                b.TourId == existingTour.Id &&
                b.SeatsCount == 2 &&
                b.TotalPrice == 2000m),
                Arg.Any<CancellationToken>());

        await bookingRepositoryMock
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());

        await eventBusMock
            .Received(1)
            .PublishAsync(
                Arg.Is<BookingCreatedEvent>(e => e.TourId == existingTour.Id),
                Arg.Any<CancellationToken>());
    }
}