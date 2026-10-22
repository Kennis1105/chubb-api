namespace Chubb.Claims.Api.Contracts.Requests;

public sealed record SettleClaimRequest(
    decimal SettledAmount,
    string DecisionNotes,
    string? OfficerId,
    string? OfficerName);
