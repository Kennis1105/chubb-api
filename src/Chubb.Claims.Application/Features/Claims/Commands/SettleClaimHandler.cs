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

public sealed class SettleClaimHandler(IClaimRepository claimRepository, IFxRateService fxRateService)
    : ICommandHandler<SettleClaimRequest, ClaimDetailResponse>
{
    public async Task<ClaimDetailResponse> HandleAsync(SettleClaimRequest request, CancellationToken cancellationToken = default)
    {
        var claim = await claimRepository.GetByIdAsync(request.ClaimId, cancellationToken)
            ?? throw new KeyNotFoundException($"Claim '{request.ClaimId}' not found.");

        if (claim.Status != ClaimStatus.UnderReview)
            throw new DomainException($"Only claims in UnderReview status can be settled (current status: {claim.Status}).");

        if (request.SettledAmount <= 0)
            throw new DomainException("Settled amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(request.DecisionNotes))
            throw new DomainException("Settlement decision notes are required.");

        var nowUtc = DateTime.UtcNow;
        var officerId = request.OfficerId ?? "off-sarah";
        var officerName = request.OfficerName ?? "Admin Officer";
        var previousReserve = claim.ActiveReserveExposure;

        claim.SettledAmount = decimal.Round(request.SettledAmount, 2);
        claim.DecisionNotes = request.DecisionNotes.Trim();
        claim.Status = ClaimStatus.Settled;
        claim.UpdatedAtUtc = nowUtc;
        claim.ClosedAtUtc = nowUtc;

        claim.AuditTrail.Add(new ClaimAuditEntry
        {
            ClaimId = claim.Id,
            Action = "CLAIM_SETTLED",
            Description = $"Claim approved and settled for {claim.Currency} {claim.SettledAmount.Value:N2}. Active reserve of {claim.Currency} {previousReserve:N2} released. Decision: {claim.DecisionNotes}",
            ActorId = officerId,
            ActorName = officerName,
            ActorRole = "ClaimsOfficer",
            OccurredAtUtc = nowUtc
        });

        claim.DomainEvents.Add(new ClaimSettledDomainEvent(
            Guid.NewGuid(),
            nowUtc,
            claim.Id,
            claim.ClaimNumber,
            claim.Market,
            claim.ClaimType,
            claim.Status,
            claim.Currency,
            claim.SettledAmount.Value,
            previousReserve,
            claim.DecisionNotes,
            officerName));

        await claimRepository.UpdateAsync(claim, cancellationToken);
        await claimRepository.SaveAsync(cancellationToken);

        return ClaimMapper.ToDetailResponse(claim, fxRateService, includeInternalNotes: true);
    }
}
