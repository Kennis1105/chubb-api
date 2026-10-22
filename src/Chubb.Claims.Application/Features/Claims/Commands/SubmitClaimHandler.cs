using Chubb.Claims.Application.Common.Dispatcher;
using Chubb.Claims.Application.Common.Interfaces;
using Chubb.Claims.Application.Contracts.Mappers;
using Chubb.Claims.Application.Contracts.Responses;
using Chubb.Claims.Domain.Common;
using Chubb.Claims.Domain.Entities;
using Chubb.Claims.Domain.Enums;

namespace Chubb.Claims.Application.Features.Claims.Commands;

public sealed record SubmitClaimCommand(
    string PolicyNumber,
    string Market,
    string ClaimType,
    string ClaimantId,
    string ClaimantName,
    string ClaimantEmail,
    string ClaimantPhone,
    DateTime IncidentDateUtc,
    string IncidentLocation,
    string IncidentDescription,
    string AssetIdentifier,
    decimal EstimatedLoss) : ICommand<ClaimDetailResponse>;

public sealed class SubmitClaimHandler(IClaimsRepository repository, IFxRateService fxRateService)
    : ICommandHandler<SubmitClaimCommand, ClaimDetailResponse>
{
    public async Task<ClaimDetailResponse> HandleAsync(SubmitClaimCommand command, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<MarketCode>(command.Market, ignoreCase: true, out var marketCode))
            throw new DomainException($"Unsupported APAC market '{command.Market}'. Valid markets: SG, HK, AU, MY, TH, ID.");

        if (!Enum.TryParse<ClaimType>(command.ClaimType, ignoreCase: true, out var claimType))
            throw new DomainException($"Unsupported claim type '{command.ClaimType}'. Valid types: Motor, Property.");

        var seq = await repository.GetNextSequenceNumberAsync(marketCode, cancellationToken);
        var claimNumber = $"CHB-{marketCode}-{DateTime.UtcNow:yyyy}-{seq:D4}";

        var claim = Claim.Submit(
            claimNumber,
            command.PolicyNumber,
            marketCode,
            claimType,
            command.ClaimantId,
            command.ClaimantName,
            command.ClaimantEmail,
            command.ClaimantPhone,
            command.IncidentDateUtc,
            command.IncidentLocation,
            command.IncidentDescription,
            command.AssetIdentifier,
            command.EstimatedLoss);

        await repository.AddAsync(claim, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return ClaimMapper.ToDetailResponse(claim, fxRateService, includeInternalNotes: false);
    }
}
