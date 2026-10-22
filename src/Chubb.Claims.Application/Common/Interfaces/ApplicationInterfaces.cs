using Chubb.Claims.Domain.Entities;
using Chubb.Claims.Domain.Enums;

namespace Chubb.Claims.Application.Common.Interfaces;

public interface IClaimsRepository
{
    Task<Claim?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Claim>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Claim claim, CancellationToken cancellationToken = default);
    Task<int> GetNextSequenceNumberAsync(MarketCode market, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OutboxMessage>> GetRecentOutboxMessagesAsync(int count = 25, CancellationToken cancellationToken = default);
}

public interface IFxRateService
{
    decimal GetUsdRate(string currencyCode);
    decimal ConvertToUsd(decimal nativeAmount, string currencyCode);
    IReadOnlyDictionary<string, decimal> GetAllRates();
}

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
