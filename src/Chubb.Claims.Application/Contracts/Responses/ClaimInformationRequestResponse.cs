namespace Chubb.Claims.Application.Contracts.Responses;

public sealed record ClaimInformationRequestResponse(
    Guid Id,
    string Question,
    string RequestedByOfficerId,
    string RequestedByOfficerName,
    DateTime RequestedAtUtc,
    string? ClaimantResponse,
    string? SupportingDocumentRef,
    DateTime? RespondedAtUtc,
    bool IsResolved);
