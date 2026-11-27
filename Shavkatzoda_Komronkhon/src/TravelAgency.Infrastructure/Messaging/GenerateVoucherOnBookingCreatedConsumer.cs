using MassTransit;
using Microsoft.Extensions.Logging;
using TravelAgency.Application.Bookings;
using TravelAgency.Application.Documents;

namespace TravelAgency.Infrastructure.Messaging;

public sealed class GenerateVoucherOnBookingCreatedConsumer(
    IDocubinetApplicationService docubinetService,
    ILogger<GenerateVoucherOnBookingCreatedConsumer> logger) : IConsumer<BookingCreatedEvent>
{
    public async Task Consume(ConsumeContext<BookingCreatedEvent> context)
    {
        var message = context.Message;

        logger.LogInformation(
            "[RabbitMQ Consumer] Получено событие BookingCreatedEvent для брони {BookingId}. Начинаем фоновую генерацию PDF-ваучера...",
            message.BookingId);

        var result = await docubinetService.GenerateBookingVoucherAsync(
            message.BookingId,
            context.CancellationToken);

        if (result.IsFailure)
        {
            logger.LogError(
                "[RabbitMQ Consumer] Не удалось сгенерировать ваучер для брони {BookingId}: {ErrorCode} - {ErrorMessage}",
                message.BookingId,
                result.Error.Code,
                result.Error.Description);

            return;
        }

        logger.LogInformation(
            "[RabbitMQ Consumer] PDF-ваучер успешно создан в фоне! DocumentId: {DocumentId}",
            result.Value.Id);
    }
}