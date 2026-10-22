using Chubb.Claims.Application.Common.Dispatcher;
using Chubb.Claims.Application.Common.Interfaces;
using Chubb.Claims.Application.Contracts.Mappers;
using Chubb.Claims.Application.Contracts.Responses;

namespace Chubb.Claims.Application.Features.Claims.Commands;

public sealed record SettleClaimCommand(
    Guid ClaimId,
    decimal SettledAmount,
    string DecisionNotes,
    string OfficerId,
    string OfficerName) : ICommand<ClaimDetailResponse>;

public sealed class SettleClaimHandler(IClaimsRepository repository, IFxRateService fxRateService)
    : ICommandHandler<SettleClaimCommand, ClaimDetailResponse>
{
    public async Task<ClaimDetailResponse> HandleAsync(SettleClaimCommand command, CancellationToken cancellationToken = default)
    {
        var claim = await repository.GetByIdAsync(command.ClaimId, cancellationToken)
            ?? throw new KeyNotFoundException($"Claim '{command.ClaimId}' not found.");

        claim.Settle(command.SettledAmount, command.DecisionNotes, command.OfficerId, command.OfficerName);
        await repository.SaveChangesAsync(cancellationToken);

        return ClaimMapper.ToDetailResponse(claim, fxRateService, includeInternalNotes: true);
    }
}
