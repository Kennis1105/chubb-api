namespace Chubb.Claims.Application.Contracts.Responses;

public sealed record PersonaMetadataResponse(
    string Id,
    string Name,
    string Role,
    string Market,
    string Title,
    string Email,
    string Phone);
