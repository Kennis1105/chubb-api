namespace Chubb.Claims.Api.Contracts.Requests;

public sealed record UpdateAssessmentRequest(
    decimal? AssessedReserve,
    string? InternalNote,
    string? OfficerId,
    string? OfficerName);
