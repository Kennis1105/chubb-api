using Chubb.Claims.Application.Common.Dispatcher;
using Chubb.Claims.Application.Common.Interfaces;
using Chubb.Claims.Application.Contracts.Mappers;
using Chubb.Claims.Application.Contracts.Requests;
using Chubb.Claims.Application.Contracts.Responses;
using Chubb.Claims.Domain.Common;
using Chubb.Claims.Domain.Entities;
using Chubb.Claims.Domain.Enums;

namespace Chubb.Claims.Application.Features.Claims.Commands;

public sealed class SubmitClaimHandler(IClaimsRepository repository, IFxRateService fxRateService)
    : ICommandHandler<SubmitClaimRequest, ClaimDetailResponse>
{
    public async Task<ClaimDetailResponse> HandleAsync(SubmitClaimRequest request, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<MarketCode>(request.Market, ignoreCase: true, out var marketCode))
            throw new DomainException($"Unsupported APAC market '{request.Market}'. Valid markets: SG, HK, AU, MY, TH, ID.");

        if (!Enum.TryParse<ClaimType>(request.ClaimType, ignoreCase: true, out var claimType))
            throw new DomainException($"Unsupported claim type '{request.ClaimType}'. Valid types: Motor, Property.");

        var seq = await repository.GetNextSequenceNumberAsync(marketCode, cancellationToken);
        var claimNumber = $"CHB-{marketCode}-{DateTime.UtcNow:yyyy}-{seq:D4}";

        var claim = Claim.Submit(
            claimNumber,
            request.PolicyNumber,
            marketCode,
            claimType,
            request.ClaimantId ?? "clm-tan",
            request.ClaimantName ?? "Tan Wei Ming",
            request.ClaimantEmail ?? "policyholder@example.com",
            request.ClaimantPhone ?? "+65 9000 0000",
            request.IncidentDateUtc == default ? DateTime.UtcNow : request.IncidentDateUtc,
            request.IncidentLocation,
            request.IncidentDescription,
            request.AssetIdentifier,
            request.EstimatedLoss);

        await repository.AddAsync(claim, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return ClaimMapper.ToDetailResponse(claim, fxRateService, includeInternalNotes: false);
    }
}
