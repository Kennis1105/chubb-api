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

public sealed class RejectClaimHandler(IClaimRepository claimRepository, IFxRateService fxRateService)
    : ICommandHandler<RejectClaimRequest, ClaimDetailResponse>
{
    public async Task<ClaimDetailResponse> HandleAsync(RejectClaimRequest request, CancellationToken cancellationToken = default)
    {
        var claim = await claimRepository.GetByIdAsync(request.ClaimId, cancellationToken)
            ?? throw new KeyNotFoundException($"Claim '{request.ClaimId}' not found.");

        if (claim.Status != ClaimStatus.UnderReview)
            throw new DomainException($"Only claims in UnderReview status can be rejected (current status: {claim.Status}).");

        if (string.IsNullOrWhiteSpace(request.RejectionReason))
            throw new DomainException("Rejection reason is required.");

        var nowUtc = DateTime.UtcNow;
        var officerId = request.OfficerId ?? "off-sarah";
        var officerName = request.OfficerName ?? "Admin Officer";
        var releasedReserve = claim.ActiveReserveExposure;

        claim.DecisionNotes = request.RejectionReason.Trim();
        claim.Status = ClaimStatus.Rejected;
        claim.UpdatedAtUtc = nowUtc;
        claim.ClosedAtUtc = nowUtc;

        claim.AuditTrail.Add(new ClaimAuditEntry
        {
            ClaimId = claim.Id,
            Action = "CLAIM_REJECTED",
            Description = $"Claim rejected. Reserve of {claim.Currency} {releasedReserve:N2} released. Reason: {claim.DecisionNotes}",
            ActorId = officerId,
            ActorName = officerName,
            ActorRole = "ClaimsOfficer",
            OccurredAtUtc = nowUtc
        });

        claim.DomainEvents.Add(new ClaimRejectedDomainEvent(
            Guid.NewGuid(),
            nowUtc,
            claim.Id,
            claim.ClaimNumber,
            claim.Market,
            claim.ClaimType,
            claim.Status,
            claim.Currency,
            releasedReserve,
            claim.DecisionNotes,
            officerName));

        await claimRepository.UpdateAsync(claim, cancellationToken);
        await claimRepository.SaveAsync(cancellationToken);

        return ClaimMapper.ToDetailResponse(claim, fxRateService, includeInternalNotes: true);
    }
}
