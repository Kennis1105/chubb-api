namespace Chubb.Claims.Application.Contracts.Responses;

public sealed record EventStreamItemResponse(
    Guid EventId,
    Guid ClaimId,
    string EventType,
    string Topic,
    string TransportMode,
    DateTime OccurredAtUtc,
    DateTime? ProcessedAtUtc,
    string SummaryJson);
