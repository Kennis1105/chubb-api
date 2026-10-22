namespace Chubb.Claims.Api.Contracts.Requests;

public sealed record RespondInfoRequest(
    string Response,
    string? SupportingDocumentRef,
    string? ClaimantId,
    string? ClaimantName);
