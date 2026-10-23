using Chubb.Claims.Application.Common.Dispatcher;
using Chubb.Claims.Application.Common.Interfaces;
using Chubb.Claims.Application.Contracts.Mappers;
using Chubb.Claims.Application.Contracts.Requests;
using Chubb.Claims.Application.Contracts.Responses;
using Chubb.Claims.Domain.Common;
using Chubb.Claims.Domain.Entities;
using Chubb.Claims.Domain.Enums;
using Chubb.Claims.Domain.Events;

namespace Chubb.Claims.Application.Features.Claims.Commands;

public sealed class SubmitClaimHandler(IClaimRepository claimRepository, IFxRateService fxRateService)
    : ICommandHandler<SubmitClaimRequest, ClaimDetailResponse>
{
    public async Task<ClaimDetailResponse> HandleAsync(SubmitClaimRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.PolicyNumber))
            throw new DomainException("Policy number is required.");
        if (string.IsNullOrWhiteSpace(request.IncidentDescription))
            throw new DomainException("Incident description is required.");
        if (request.EstimatedLoss <= 0)
            throw new DomainException("Estimated loss must be greater than zero.");

        if (!Enum.TryParse<MarketCode>(request.Market, ignoreCase: true, out var marketCode))
            throw new DomainException($"Unsupported APAC market '{request.Market}'. Valid markets: SG, HK, AU, MY, TH, ID.");

        if (!Enum.TryParse<ClaimType>(request.ClaimType, ignoreCase: true, out var claimType))
            throw new DomainException($"Unsupported claim type '{request.ClaimType}'. Valid types: Motor, Property.");

        var seq = await claimRepository.GetNextSequenceNumberAsync(marketCode, cancellationToken);
        var nowUtc = DateTime.UtcNow;
        var currency = MarketMetadata.GetCurrency(marketCode);
        var claimantId = request.ClaimantId ?? "clm-tan";
        var claimantName = (request.ClaimantName ?? "Lim Hean Soon").Trim();

        var claim = new Claim
        {
            Id = Guid.NewGuid(),
            ClaimNumber = $"CHB-{marketCode}-{nowUtc:yyyy}-{seq:D4}",
            PolicyNumber = request.PolicyNumber.Trim().ToUpperInvariant(),
            Market = marketCode,
            ClaimType = claimType,
            Status = ClaimStatus.Submitted,
            ClaimantId = claimantId,
            ClaimantName = claimantName,
            ClaimantEmail = (request.ClaimantEmail ?? "policyholder@example.com").Trim(),
            ClaimantPhone = (request.ClaimantPhone ?? "+65 9000 0000").Trim(),
            IncidentDateUtc = request.IncidentDateUtc == default ? nowUtc : request.IncidentDateUtc,
            IncidentLocation = request.IncidentLocation.Trim(),
            IncidentDescription = request.IncidentDescription.Trim(),
            AssetIdentifier = request.AssetIdentifier.Trim(),
            Currency = currency,
            EstimatedLoss = decimal.Round(request.EstimatedLoss, 2),
            SubmittedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };

        claim.AuditTrail.Add(new ClaimAuditEntry
        {
            ClaimId = claim.Id,
            Action = "CLAIM_SUBMITTED",
            Description = $"FNOL reported for {claimType} policy {claim.PolicyNumber} in {marketCode} ({currency} {claim.EstimatedLoss:N2}).",
            ActorId = claimantId,
            ActorName = claimantName,
            ActorRole = "Claimant",
            OccurredAtUtc = nowUtc
        });

        claim.DomainEvents.Add(new ClaimSubmittedDomainEvent(
            Guid.NewGuid(),
            nowUtc,
            claim.Id,
            claim.ClaimNumber,
            claim.Market,
            claim.ClaimType,
            claim.Status,
            claim.Currency,
            claim.EstimatedLoss,
            claim.ActiveReserveExposure,
            claim.ClaimantName));

        await claimRepository.AddAsync(claim, cancellationToken);
        await claimRepository.SaveAsync(cancellationToken);

        return ClaimMapper.ToDetailResponse(claim, fxRateService, includeInternalNotes: false);
    }
}
