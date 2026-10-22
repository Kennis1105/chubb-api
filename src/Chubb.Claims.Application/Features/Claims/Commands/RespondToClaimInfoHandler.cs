using Chubb.Claims.Application.Common.Dispatcher;
using Chubb.Claims.Application.Common.Interfaces;
using Chubb.Claims.Application.Contracts.Mappers;
using Chubb.Claims.Application.Contracts.Responses;

namespace Chubb.Claims.Application.Features.Claims.Commands;

public sealed record RespondToClaimInfoCommand(
    Guid ClaimId,
    string Response,
    string? SupportingDocumentRef,
    string ClaimantId,
    string ClaimantName) : ICommand<ClaimDetailResponse>;

public sealed class RespondToClaimInfoHandler(IClaimsRepository repository, IFxRateService fxRateService)
    : ICommandHandler<RespondToClaimInfoCommand, ClaimDetailResponse>
{
    public async Task<ClaimDetailResponse> HandleAsync(RespondToClaimInfoCommand command, CancellationToken cancellationToken = default)
    {
        var claim = await repository.GetByIdAsync(command.ClaimId, cancellationToken)
            ?? throw new KeyNotFoundException($"Claim '{command.ClaimId}' not found.");

        claim.RespondToInformationRequest(command.Response, command.SupportingDocumentRef, command.ClaimantId, command.ClaimantName);
        await repository.SaveChangesAsync(cancellationToken);

        return ClaimMapper.ToDetailResponse(claim, fxRateService, includeInternalNotes: false);
    }
}
