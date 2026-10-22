namespace Chubb.Claims.Application.Contracts.Responses;

public sealed record ExposureAnalyticsResponse(
    decimal TotalActiveLiabilityExposureUsd,
    decimal TotalSettledPayoutsUsd,
    decimal TotalReleasedReservesUsd,
    int TotalClaimsCount,
    int OpenClaimsCount,
    int UnassignedClaimsCount,
    int AwaitingClaimantInfoCount,
    int SettledClaimsCount,
    int RejectedClaimsCount,
    IReadOnlyList<MarketExposureBreakdownResponse> ByMarket,
    IReadOnlyList<ClaimTypeBreakdownResponse> ByClaimType,
    IReadOnlyList<OfficerWorkloadResponse> TeamWorkload,
    IReadOnlyList<EventStreamItemResponse> RecentEventStream);
