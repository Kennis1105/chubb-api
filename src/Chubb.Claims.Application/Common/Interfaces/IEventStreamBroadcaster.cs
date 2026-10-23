namespace Chubb.Claims.Application.Common.Interfaces;

public sealed record IntegrationEventEnvelope(
    Guid EventId,
    Guid ClaimId,
    string EventType,
    string Topic,
    string PayloadJson,
    DateTime OccurredAtUtc,
    string TransportMode);

public interface IEventStreamBroadcaster
{
    Task PublishToLocalStreamAsync(IntegrationEventEnvelope envelope, CancellationToken cancellationToken = default);
    IReadOnlyList<IntegrationEventEnvelope> GetRecentPublishedEvents(int count = 30);
}
