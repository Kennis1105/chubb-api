using Chubb.Claims.Application.Common.Dispatcher;
using Chubb.Claims.Application.Common.Interfaces;
using Chubb.Claims.Application.Contracts.Mappers;
using Chubb.Claims.Application.Contracts.Responses;

namespace Chubb.Claims.Application.Features.Claims.Commands;

public sealed record RequestClaimInfoCommand(
    Guid ClaimId,
    string Question,
    string OfficerId,
    string OfficerName) : ICommand<ClaimDetailResponse>;

public sealed class RequestClaimInfoHandler(IClaimsRepository repository, IFxRateService fxRateService)
    : ICommandHandler<RequestClaimInfoCommand, ClaimDetailResponse>
{
    public async Task<ClaimDetailResponse> HandleAsync(RequestClaimInfoCommand command, CancellationToken cancellationToken = default)
    {
        var claim = await repository.GetByIdAsync(command.ClaimId, cancellationToken)
            ?? throw new KeyNotFoundException($"Claim '{command.ClaimId}' not found.");

        claim.RequestAdditionalInfo(command.Question, command.OfficerId, command.OfficerName);
        await repository.SaveChangesAsync(cancellationToken);

        return ClaimMapper.ToDetailResponse(claim, fxRateService, includeInternalNotes: true);
    }
}
