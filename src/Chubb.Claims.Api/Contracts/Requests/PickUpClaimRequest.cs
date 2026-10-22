namespace Chubb.Claims.Api.Contracts.Requests;

public sealed record PickUpClaimRequest(
    string? OfficerId,
    string? OfficerName);
