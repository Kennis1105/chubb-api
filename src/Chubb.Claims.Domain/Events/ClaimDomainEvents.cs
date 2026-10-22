using Chubb.Claims.Domain.Common;
using Chubb.Claims.Domain.Enums;

namespace Chubb.Claims.Domain.Events;

public abstract record ClaimDomainEventBase(
    Guid EventId,
    DateTime OccurredAtUtc,
    Guid ClaimId,
    string ClaimNumber,
    MarketCode Market,
    ClaimType ClaimType,
    ClaimStatus NewStatus,
    string Currency,
    decimal ActiveReserveExposure,
    string ActorName) : IDomainEvent
{
    public string EventType => GetType().Name;
}

public sealed record ClaimSubmittedDomainEvent(
    Guid EventId,
    DateTime OccurredAtUtc,
    Guid ClaimId,
    string ClaimNumber,
    MarketCode Market,
    ClaimType ClaimType,
    ClaimStatus NewStatus,
    string Currency,
    decimal EstimatedLoss,
    decimal ActiveReserveExposure,
    string ClaimantName)
    : ClaimDomainEventBase(EventId, OccurredAtUtc, ClaimId, ClaimNumber, Market, ClaimType, NewStatus, Currency, ActiveReserveExposure, ClaimantName);

public sealed record ClaimPickedUpDomainEvent(
    Guid EventId,
    DateTime OccurredAtUtc,
    Guid ClaimId,
    string ClaimNumber,
    MarketCode Market,
    ClaimType ClaimType,
    ClaimStatus NewStatus,
    string Currency,
    decimal ActiveReserveExposure,
    string OfficerId,
    string OfficerName)
    : ClaimDomainEventBase(EventId, OccurredAtUtc, ClaimId, ClaimNumber, Market, ClaimType, NewStatus, Currency, ActiveReserveExposure, OfficerName);

public sealed record ClaimReserveUpdatedDomainEvent(
    Guid EventId,
    DateTime OccurredAtUtc,
    Guid ClaimId,
    string ClaimNumber,
    MarketCode Market,
    ClaimType ClaimType,
    ClaimStatus NewStatus,
    string Currency,
    decimal PreviousReserve,
    decimal NewAssessedReserve,
    decimal ActiveReserveExposure,
    string OfficerName)
    : ClaimDomainEventBase(EventId, OccurredAtUtc, ClaimId, ClaimNumber, Market, ClaimType, NewStatus, Currency, ActiveReserveExposure, OfficerName);

public sealed record ClaimInfoRequestedDomainEvent(
    Guid EventId,
    DateTime OccurredAtUtc,
    Guid ClaimId,
    string ClaimNumber,
    MarketCode Market,
    ClaimType ClaimType,
    ClaimStatus NewStatus,
    string Currency,
    decimal ActiveReserveExposure,
    string Question,
    string OfficerName)
    : ClaimDomainEventBase(EventId, OccurredAtUtc, ClaimId, ClaimNumber, Market, ClaimType, NewStatus, Currency, ActiveReserveExposure, OfficerName);

public sealed record ClaimInfoRespondedDomainEvent(
    Guid EventId,
    DateTime OccurredAtUtc,
    Guid ClaimId,
    string ClaimNumber,
    MarketCode Market,
    ClaimType ClaimType,
    ClaimStatus NewStatus,
    string Currency,
    decimal ActiveReserveExposure,
    string ResponseSummary,
    string ClaimantName)
    : ClaimDomainEventBase(EventId, OccurredAtUtc, ClaimId, ClaimNumber, Market, ClaimType, NewStatus, Currency, ActiveReserveExposure, ClaimantName);

public sealed record ClaimSettledDomainEvent(
    Guid EventId,
    DateTime OccurredAtUtc,
    Guid ClaimId,
    string ClaimNumber,
    MarketCode Market,
    ClaimType ClaimType,
    ClaimStatus NewStatus,
    string Currency,
    decimal SettledAmount,
    decimal PreviousReserveReleased,
    string DecisionNotes,
    string OfficerName)
    : ClaimDomainEventBase(EventId, OccurredAtUtc, ClaimId, ClaimNumber, Market, ClaimType, NewStatus, Currency, 0m, OfficerName);

public sealed record ClaimRejectedDomainEvent(
    Guid EventId,
    DateTime OccurredAtUtc,
    Guid ClaimId,
    string ClaimNumber,
    MarketCode Market,
    ClaimType ClaimType,
    ClaimStatus NewStatus,
    string Currency,
    decimal ReleasedReserveAmount,
    string RejectionReason,
    string OfficerName)
    : ClaimDomainEventBase(EventId, OccurredAtUtc, ClaimId, ClaimNumber, Market, ClaimType, NewStatus, Currency, 0m, OfficerName);
