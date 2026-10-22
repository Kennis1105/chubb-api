using Chubb.Claims.Application.Common.Dispatcher;
using Chubb.Claims.Application.Common.Interfaces;
using Chubb.Claims.Application.Contracts.Mappers;
using Chubb.Claims.Application.Contracts.Responses;

namespace Chubb.Claims.Application.Features.Claims.Queries;

public sealed record GetClaimByIdQuery(Guid ClaimId, bool IsStaffRole) : IQuery<ClaimDetailResponse>;

public sealed class GetClaimByIdHandler(IClaimsRepository repository, IFxRateService fxRateService)
    : IQueryHandler<GetClaimByIdQuery, ClaimDetailResponse>
{
    public async Task<ClaimDetailResponse> HandleAsync(GetClaimByIdQuery query, CancellationToken cancellationToken = default)
    {
        var claim = await repository.GetByIdAsync(query.ClaimId, cancellationToken)
            ?? throw new KeyNotFoundException($"Claim '{query.ClaimId}' not found.");

        return ClaimMapper.ToDetailResponse(claim, fxRateService, includeInternalNotes: query.IsStaffRole);
    }
}
