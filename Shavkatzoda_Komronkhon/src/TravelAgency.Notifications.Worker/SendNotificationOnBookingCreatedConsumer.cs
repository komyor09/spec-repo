using MassTransit;
using TravelAgency.Application.Bookings;

namespace TravelAgency.Notifications.Worker;

public sealed class SendNotificationOnBookingCreatedConsumer(
    ILogger<SendNotificationOnBookingCreatedConsumer> logger) : IConsumer<BookingCreatedEvent>
{
    public async Task Consume(ConsumeContext<BookingCreatedEvent> context)
    {
        var message = context.Message;

        logger.LogInformation(
            "📧 [Notifications Microservice] Получено событие бронирования {BookingId}. Готовим приветственное письмо для {CustomerEmail}...",
            message.BookingId,
            message.CustomerEmail);

        // Имитируем обращение к внешнему почтовому SMTP-шлюзу или Telegram Bot API
        await Task.Delay(500, context.CancellationToken);

        logger.LogInformation(
            "✅ [Notifications Microservice] Уведомление для {CustomerEmail} по туру {TourId} успешно отправлено!",
            message.CustomerEmail,
            message.TourId);
    }
}