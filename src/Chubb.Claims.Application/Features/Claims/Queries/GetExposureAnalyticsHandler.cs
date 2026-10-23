using Chubb.Claims.Application.Common.Dispatcher;
using Chubb.Claims.Application.Common.Interfaces;
using Chubb.Claims.Application.Contracts.Requests;
using Chubb.Claims.Application.Contracts.Responses;
using Chubb.Claims.Domain.Enums;

namespace Chubb.Claims.Application.Features.Claims.Queries;

public sealed class GetExposureAnalyticsHandler(IClaimRepository repository, IFxRateService fxRateService)
    : IQueryHandler<GetExposureAnalyticsRequest, ExposureAnalyticsResponse>
{
    private static readonly (string OfficerId, string OfficerName, string PrimaryMarkets)[] KnownOfficers =
    [
        ("off-sarah", "Admin Officer", "APAC (SG, HK, AU, MY, TH, ID)")
    ];

    public async Task<ExposureAnalyticsResponse> HandleAsync(GetExposureAnalyticsRequest query, CancellationToken cancellationToken = default)
    {
        var claims = await repository.GetAllWithDetailsAsync(cancellationToken);
        var outboxMessages = await repository.GetRecentOutboxMessagesAsync(15, cancellationToken);

        var totalActiveUsd = claims.Sum(c => fxRateService.ConvertToUsd(c.ActiveReserveExposure, c.Currency));
        var totalSettledUsd = claims.Sum(c => fxRateService.ConvertToUsd(c.RealizedPayout, c.Currency));
        var totalReleasedUsd = claims.Sum(c => fxRateService.ConvertToUsd(c.ReleasedReserve, c.Currency));

        var openClaims = claims.Where(c => c.Status is ClaimStatus.Submitted or ClaimStatus.UnderReview or ClaimStatus.InfoRequested).ToList();

        var byMarket = Enum.GetValues<MarketCode>()
            .Select(market =>
            {
                var marketClaims = claims.Where(c => c.Market == market).ToList();
                var currency = MarketMetadata.GetCurrency(market);
                var fxRate = fxRateService.GetUsdRate(currency);
                var activeNative = marketClaims.Sum(c => c.ActiveReserveExposure);
                var settledNative = marketClaims.Sum(c => c.RealizedPayout);

                return new MarketExposureBreakdownResponse(
                    market.ToString(),
                    MarketMetadata.GetCountryName(market),
                    currency,
                    fxRate,
                    marketClaims.Count,
                    marketClaims.Count(c => c.Status is ClaimStatus.Submitted or ClaimStatus.UnderReview or ClaimStatus.InfoRequested),
                    activeNative,
                    fxRateService.ConvertToUsd(activeNative, currency),
                    settledNative,
                    fxRateService.ConvertToUsd(settledNative, currency));
            })
            .Where(m => m.TotalClaimsCount > 0)
            .OrderByDescending(m => m.ActiveReserveUsd)
            .ToList();

        var byClaimType = Enum.GetValues<ClaimType>()
            .Select(type =>
            {
                var typeClaims = claims.Where(c => c.ClaimType == type).ToList();
                return new ClaimTypeBreakdownResponse(
                    type.ToString(),
                    typeClaims.Count(c => c.Status is ClaimStatus.Submitted or ClaimStatus.UnderReview or ClaimStatus.InfoRequested),
                    typeClaims.Count,
                    typeClaims.Sum(c => fxRateService.ConvertToUsd(c.ActiveReserveExposure, c.Currency)),
                    typeClaims.Sum(c => fxRateService.ConvertToUsd(c.RealizedPayout, c.Currency)));
            })
            .ToList();

        var officerGroups = KnownOfficers.Select(officer =>
        {
            var assigned = claims.Where(c => string.Equals(c.AssignedOfficerId, officer.OfficerId, StringComparison.OrdinalIgnoreCase)).ToList();
            var closed = assigned.Where(c => c.ClosedAtUtc.HasValue).ToList();
            var avgHours = closed.Count > 0
                ? Math.Round(closed.Average(c => (c.ClosedAtUtc!.Value - c.SubmittedAtUtc).TotalHours), 1)
                : 12.0;

            return new OfficerWorkloadResponse(
                officer.OfficerId,
                officer.OfficerName,
                officer.PrimaryMarkets,
                assigned.Count(c => c.Status == ClaimStatus.UnderReview),
                assigned.Count(c => c.Status == ClaimStatus.InfoRequested),
                assigned.Count(c => c.Status == ClaimStatus.Settled),
                assigned.Count(c => c.Status == ClaimStatus.Rejected),
                assigned.Sum(c => fxRateService.ConvertToUsd(c.ActiveReserveExposure, c.Currency)),
                avgHours);
        }).ToList();

        var eventStream = outboxMessages.Select(m => new EventStreamItemResponse(
            m.Id,
            m.AggregateId,
            m.EventType,
            m.Topic,
            m.PublishTransport ?? "PendingOutbox",
            m.OccurredAtUtc,
            m.ProcessedAtUtc,
            m.PayloadJson)).ToList();

        return new ExposureAnalyticsResponse(
            totalActiveUsd,
            totalSettledUsd,
            totalReleasedUsd,
            claims.Count,
            openClaims.Count,
            claims.Count(c => c.Status == ClaimStatus.Submitted && string.IsNullOrEmpty(c.AssignedOfficerId)),
            claims.Count(c => c.Status == ClaimStatus.InfoRequested),
            claims.Count(c => c.Status == ClaimStatus.Settled),
            claims.Count(c => c.Status == ClaimStatus.Rejected),
            byMarket,
            byClaimType,
            officerGroups,
            eventStream);
    }
}
