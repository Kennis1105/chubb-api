using Chubb.Claims.Application.Common.Dispatcher;
using Chubb.Claims.Application.Common.Interfaces;
using Chubb.Claims.Application.DTOs;
using Chubb.Claims.Domain.Enums;

namespace Chubb.Claims.Application.Features.Claims.Queries;

// 1. Get Claims List with multi-dimensional filtering
public sealed record GetClaimsListQuery(
    string? Market = null,
    string? ClaimType = null,
    string? Status = null,
    string? ClaimantId = null,
    string? AssignedOfficerId = null,
    string? Search = null) : IQuery<IReadOnlyList<ClaimSummaryDto>>;

public sealed class GetClaimsListHandler(IClaimsRepository repository, IFxRateService fxRateService)
    : IQueryHandler<GetClaimsListQuery, IReadOnlyList<ClaimSummaryDto>>
{
    public async Task<IReadOnlyList<ClaimSummaryDto>> HandleAsync(GetClaimsListQuery query, CancellationToken cancellationToken = default)
    {
        var allClaims = await repository.GetAllWithDetailsAsync(cancellationToken);
        var filtered = allClaims.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query.Market) && Enum.TryParse<MarketCode>(query.Market, true, out var market))
            filtered = filtered.Where(c => c.Market == market);

        if (!string.IsNullOrWhiteSpace(query.ClaimType) && Enum.TryParse<ClaimType>(query.ClaimType, true, out var claimType))
            filtered = filtered.Where(c => c.ClaimType == claimType);

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (query.Status.Equals("Open", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(c => c.Status is ClaimStatus.Submitted or ClaimStatus.UnderReview or ClaimStatus.InfoRequested);
            }
            else if (Enum.TryParse<ClaimStatus>(query.Status, true, out var status))
            {
                filtered = filtered.Where(c => c.Status == status);
            }
        }

        if (!string.IsNullOrWhiteSpace(query.ClaimantId))
            filtered = filtered.Where(c => c.ClaimantId.Equals(query.ClaimantId, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(query.AssignedOfficerId))
        {
            if (query.AssignedOfficerId.Equals("UNASSIGNED", StringComparison.OrdinalIgnoreCase))
                filtered = filtered.Where(c => string.IsNullOrEmpty(c.AssignedOfficerId));
            else
                filtered = filtered.Where(c => string.Equals(c.AssignedOfficerId, query.AssignedOfficerId, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            filtered = filtered.Where(c =>
                c.ClaimNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                c.PolicyNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                c.ClaimantName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                c.IncidentLocation.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                c.AssetIdentifier.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        return filtered
            .OrderByDescending(c => c.UpdatedAtUtc)
            .Select(c => ClaimMapper.ToSummaryDto(c, fxRateService))
            .ToList();
    }
}

// 2. Get Claim Detail By ID (Masks internal officer notes when queried by a Claimant!)
public sealed record GetClaimByIdQuery(Guid ClaimId, bool IsStaffRole) : IQuery<ClaimDetailDto>;

public sealed class GetClaimByIdHandler(IClaimsRepository repository, IFxRateService fxRateService)
    : IQueryHandler<GetClaimByIdQuery, ClaimDetailDto>
{
    public async Task<ClaimDetailDto> HandleAsync(GetClaimByIdQuery query, CancellationToken cancellationToken = default)
    {
        var claim = await repository.GetByIdAsync(query.ClaimId, cancellationToken)
            ?? throw new KeyNotFoundException($"Claim '{query.ClaimId}' not found.");

        return ClaimMapper.ToDetailDto(claim, fxRateService, includeInternalNotes: query.IsStaffRole);
    }
}

// 3. Get Real-Time APAC Liability Exposure & Team Workload Analytics
public sealed record GetExposureAnalyticsQuery() : IQuery<ExposureAnalyticsDto>;

public sealed class GetExposureAnalyticsHandler(IClaimsRepository repository, IFxRateService fxRateService)
    : IQueryHandler<GetExposureAnalyticsQuery, ExposureAnalyticsDto>
{
    private static readonly (string OfficerId, string OfficerName, string PrimaryMarkets)[] KnownOfficers =
    [
        ("off-sarah", "Sarah Jenkins", "SG, HK"),
        ("off-raj", "Rajesh Patel", "AU, MY"),
        ("off-narong", "Narong Srisai", "TH, ID")
    ];

    public async Task<ExposureAnalyticsDto> HandleAsync(GetExposureAnalyticsQuery query, CancellationToken cancellationToken = default)
    {
        var claims = await repository.GetAllWithDetailsAsync(cancellationToken);
        var outboxMessages = await repository.GetRecentOutboxMessagesAsync(25, cancellationToken);

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

                return new MarketExposureBreakdownDto(
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
            .OrderByDescending(m => m.ActiveReserveUsd)
            .ToList();

        var byClaimType = Enum.GetValues<ClaimType>()
            .Select(type =>
            {
                var typeClaims = claims.Where(c => c.ClaimType == type).ToList();
                return new ClaimTypeBreakdownDto(
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
                : 18.5;

            return new OfficerWorkloadDto(
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

        var eventStream = outboxMessages.Select(m => new EventStreamItemDto(
            m.Id,
            m.AggregateId,
            m.EventType,
            m.Topic,
            m.PublishTransport ?? "PendingOutbox",
            m.OccurredAtUtc,
            m.ProcessedAtUtc,
            m.PayloadJson)).ToList();

        return new ExposureAnalyticsDto(
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
