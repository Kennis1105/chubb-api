namespace Chubb.Claims.Api.Contracts.Requests;

public sealed record RejectClaimRequest(
    string RejectionReason,
    string? OfficerId,
    string? OfficerName);
