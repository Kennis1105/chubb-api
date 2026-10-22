using Chubb.Claims.Application.Common.Dispatcher;
using Chubb.Claims.Application.Common.Interfaces;
using Chubb.Claims.Application.Contracts.Mappers;
using Chubb.Claims.Application.Contracts.Requests;
using Chubb.Claims.Application.Contracts.Responses;

namespace Chubb.Claims.Application.Features.Claims.Commands;

public sealed class UpdateClaimAssessmentHandler(IClaimsRepository repository, IFxRateService fxRateService)
    : ICommandHandler<UpdateAssessmentRequest, ClaimDetailResponse>
{
    public async Task<ClaimDetailResponse> HandleAsync(UpdateAssessmentRequest request, CancellationToken cancellationToken = default)
    {
        var claim = await repository.GetByIdAsync(request.ClaimId, cancellationToken)
            ?? throw new KeyNotFoundException($"Claim '{request.ClaimId}' not found.");

        claim.UpdateAssessment(
            request.AssessedReserve,
            request.InternalNote,
            request.OfficerId ?? "off-sarah",
            request.OfficerName ?? "Sarah Jenkins");
        await repository.SaveChangesAsync(cancellationToken);

        return ClaimMapper.ToDetailResponse(claim, fxRateService, includeInternalNotes: true);
    }
}
