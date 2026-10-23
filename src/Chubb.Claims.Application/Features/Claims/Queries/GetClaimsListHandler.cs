using Chubb.Claims.Application.Common.Dispatcher;
using Chubb.Claims.Application.Common.Interfaces;
using Chubb.Claims.Application.Contracts.Mappers;
using Chubb.Claims.Application.Contracts.Requests;
using Chubb.Claims.Application.Contracts.Responses;
using Chubb.Claims.Domain.Enums;

namespace Chubb.Claims.Application.Features.Claims.Queries;

public sealed class GetClaimsListHandler(IClaimRepository repository, IFxRateService fxRateService)
    : IQueryHandler<GetClaimsListRequest, IReadOnlyList<ClaimSummaryResponse>>
{
    public async Task<IReadOnlyList<ClaimSummaryResponse>> HandleAsync(GetClaimsListRequest query, CancellationToken cancellationToken = default)
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
            .Select(c => ClaimMapper.ToSummaryResponse(c, fxRateService))
            .ToList();
    }
}
