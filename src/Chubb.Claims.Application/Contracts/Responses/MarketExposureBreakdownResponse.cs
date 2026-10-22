namespace Chubb.Claims.Application.Contracts.Responses;

public sealed record MarketExposureBreakdownResponse(
    string Market,
    string CountryName,
    string Currency,
    decimal FxRateToUsd,
    int TotalClaimsCount,
    int OpenClaimsCount,
    decimal ActiveReserveNative,
    decimal ActiveReserveUsd,
    decimal SettledPayoutNative,
    decimal SettledPayoutUsd);
