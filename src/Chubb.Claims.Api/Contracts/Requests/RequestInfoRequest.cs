namespace Chubb.Claims.Api.Contracts.Requests;

public sealed record RequestInfoRequest(
    string Question,
    string? OfficerId,
    string? OfficerName);
