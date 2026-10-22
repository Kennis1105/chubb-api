using Chubb.Claims.Application.Common.Dispatcher;
using Chubb.Claims.Application.Common.Interfaces;
using Chubb.Claims.Application.Contracts.Mappers;
using Chubb.Claims.Application.Contracts.Requests;
using Chubb.Claims.Application.Contracts.Responses;

namespace Chubb.Claims.Application.Features.Claims.Commands;

public sealed class RequestClaimInfoHandler(IClaimsRepository repository, IFxRateService fxRateService)
    : ICommandHandler<RequestInfoRequest, ClaimDetailResponse>
{
    public async Task<ClaimDetailResponse> HandleAsync(RequestInfoRequest request, CancellationToken cancellationToken = default)
    {
        var claim = await repository.GetByIdAsync(request.ClaimId, cancellationToken)
            ?? throw new KeyNotFoundException($"Claim '{request.ClaimId}' not found.");

        claim.RequestAdditionalInfo(
            request.Question,
            request.OfficerId ?? "off-sarah",
            request.OfficerName ?? "Sarah Jenkins");
        await repository.SaveChangesAsync(cancellationToken);

        return ClaimMapper.ToDetailResponse(claim, fxRateService, includeInternalNotes: true);
    }
}
