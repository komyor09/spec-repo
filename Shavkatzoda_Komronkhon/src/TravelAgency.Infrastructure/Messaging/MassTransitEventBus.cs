using MassTransit;
using TravelAgency.Application.Interfaces;

namespace TravelAgency.Infrastructure.Messaging;

public sealed class MassTransitEventBus(IPublishEndpoint publishEndpoint) : IEventBus
{
    public async Task PublishAsync<TEvent>(TEvent message, CancellationToken ct = default)
        where TEvent : class
    {
        await publishEndpoint.Publish(message, ct);
    }
}