namespace Chubb.Claims.Api.Contracts;

public sealed record SubmitClaimRequest(
    string PolicyNumber,
    string Market,
    string ClaimType,
    string? ClaimantId,
    string? ClaimantName,
    string? ClaimantEmail,
    string? ClaimantPhone,
    DateTime IncidentDateUtc,
    string IncidentLocation,
    string IncidentDescription,
    string AssetIdentifier,
    decimal EstimatedLoss);

public sealed record PickUpClaimRequest(
    string? OfficerId,
    string? OfficerName);

public sealed record UpdateAssessmentRequest(
    decimal? AssessedReserve,
    string? InternalNote,
    string? OfficerId,
    string? OfficerName);

public sealed record RequestInfoRequest(
    string Question,
    string? OfficerId,
    string? OfficerName);

public sealed record RespondInfoRequest(
    string Response,
    string? SupportingDocumentRef,
    string? ClaimantId,
    string? ClaimantName);

public sealed record SettleClaimRequest(
    decimal SettledAmount,
    string DecisionNotes,
    string? OfficerId,
    string? OfficerName);

public sealed record RejectClaimRequest(
    string RejectionReason,
    string? OfficerId,
    string? OfficerName);
