namespace Chubb.Claims.Application.Contracts.Responses;

public sealed record ClaimAuditEntryResponse(
    Guid Id,
    string Action,
    string Description,
    string ActorId,
    string ActorName,
    string ActorRole,
    DateTime OccurredAtUtc);
