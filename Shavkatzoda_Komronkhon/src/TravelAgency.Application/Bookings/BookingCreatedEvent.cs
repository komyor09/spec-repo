namespace TravelAgency.Application.Bookings;

public sealed record BookingCreatedEvent(
    Guid BookingId,
    Guid TourId,
    string CustomerEmail,
    DateTime CreatedAtUtc);