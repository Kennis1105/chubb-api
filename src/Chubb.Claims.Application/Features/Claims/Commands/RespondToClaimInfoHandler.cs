using Chubb.Claims.Application.Common.Dispatcher;
using Chubb.Claims.Application.Common.Interfaces;
using Chubb.Claims.Application.Contracts.Mappers;
using Chubb.Claims.Application.Contracts.Requests;
using Chubb.Claims.Application.Contracts.Responses;

namespace Chubb.Claims.Application.Features.Claims.Commands;

public sealed class RespondToClaimInfoHandler(IClaimsRepository repository, IFxRateService fxRateService)
    : ICommandHandler<RespondInfoRequest, ClaimDetailResponse>
{
    public async Task<ClaimDetailResponse> HandleAsync(RespondInfoRequest request, CancellationToken cancellationToken = default)
    {
        var claim = await repository.GetByIdAsync(request.ClaimId, cancellationToken)
            ?? throw new KeyNotFoundException($"Claim '{request.ClaimId}' not found.");

        claim.RespondToInformationRequest(
            request.Response,
            request.SupportingDocumentRef,
            request.ClaimantId ?? "clm-tan",
            request.ClaimantName ?? "Tan Wei Ming");
        await repository.SaveChangesAsync(cancellationToken);

        return ClaimMapper.ToDetailResponse(claim, fxRateService, includeInternalNotes: false);
    }
}
