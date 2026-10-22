using Chubb.Claims.Application.Common.Dispatcher;
using Chubb.Claims.Application.Common.Interfaces;
using Chubb.Claims.Application.Contracts.Mappers;
using Chubb.Claims.Application.Contracts.Responses;

namespace Chubb.Claims.Application.Features.Claims.Commands;

public sealed record RejectClaimCommand(
    Guid ClaimId,
    string RejectionReason,
    string OfficerId,
    string OfficerName) : ICommand<ClaimDetailResponse>;

public sealed class RejectClaimHandler(IClaimsRepository repository, IFxRateService fxRateService)
    : ICommandHandler<RejectClaimCommand, ClaimDetailResponse>
{
    public async Task<ClaimDetailResponse> HandleAsync(RejectClaimCommand command, CancellationToken cancellationToken = default)
    {
        var claim = await repository.GetByIdAsync(command.ClaimId, cancellationToken)
            ?? throw new KeyNotFoundException($"Claim '{command.ClaimId}' not found.");

        claim.Reject(command.RejectionReason, command.OfficerId, command.OfficerName);
        await repository.SaveChangesAsync(cancellationToken);

        return ClaimMapper.ToDetailResponse(claim, fxRateService, includeInternalNotes: true);
    }
}
