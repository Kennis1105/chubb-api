namespace Chubb.Claims.Application.Contracts.Responses;

public sealed record ClaimTypeBreakdownResponse(
    string ClaimType,
    int OpenClaimsCount,
    int TotalClaimsCount,
    decimal ActiveReserveUsd,
    decimal SettledPayoutUsd);
