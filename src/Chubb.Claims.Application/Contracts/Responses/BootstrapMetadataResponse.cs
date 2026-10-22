namespace Chubb.Claims.Application.Contracts.Responses;

public sealed record BootstrapMetadataResponse(
    IReadOnlyList<MarketMetadataResponse> Markets,
    IReadOnlyList<string> ClaimTypes,
    IReadOnlyList<string> ClaimStatuses,
    IReadOnlyList<PersonaMetadataResponse> Personas);
