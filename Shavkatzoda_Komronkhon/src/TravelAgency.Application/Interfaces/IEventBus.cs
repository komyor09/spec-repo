namespace TravelAgency.Application.Interfaces;

public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent message, CancellationToken ct = default)
        where TEvent : class;
}