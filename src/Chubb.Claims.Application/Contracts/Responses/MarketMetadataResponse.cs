namespace Chubb.Claims.Application.Contracts.Responses;

public sealed record MarketMetadataResponse(
    string Code,
    string CountryName,
    string Currency,
    decimal FxRateToUsd);
