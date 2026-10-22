using Chubb.Claims.Application.Common.Dispatcher;
using Chubb.Claims.Application.Common.Interfaces;
using Chubb.Claims.Application.Contracts.Mappers;
using Chubb.Claims.Application.Contracts.Responses;

namespace Chubb.Claims.Application.Features.Claims.Commands;

public sealed record UpdateClaimAssessmentCommand(
    Guid ClaimId,
    decimal? AssessedReserve,
    string? InternalNote,
    string OfficerId,
    string OfficerName) : ICommand<ClaimDetailResponse>;

public sealed class UpdateClaimAssessmentHandler(IClaimsRepository repository, IFxRateService fxRateService)
    : ICommandHandler<UpdateClaimAssessmentCommand, ClaimDetailResponse>
{
    public async Task<ClaimDetailResponse> HandleAsync(UpdateClaimAssessmentCommand command, CancellationToken cancellationToken = default)
    {
        var claim = await repository.GetByIdAsync(command.ClaimId, cancellationToken)
            ?? throw new KeyNotFoundException($"Claim '{command.ClaimId}' not found.");

        claim.UpdateAssessment(command.AssessedReserve, command.InternalNote, command.OfficerId, command.OfficerName);
        await repository.SaveChangesAsync(cancellationToken);

        return ClaimMapper.ToDetailResponse(claim, fxRateService, includeInternalNotes: true);
    }
}
