namespace Chubb.Claims.Api.Contracts.Requests;

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
