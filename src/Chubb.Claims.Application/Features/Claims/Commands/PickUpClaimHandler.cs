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

public sealed class PickUpClaimHandler(IClaimRepository claimRepository, IFxRateService fxRateService)
    : ICommandHandler<PickUpClaimRequest, ClaimDetailResponse>
{
    public async Task<ClaimDetailResponse> HandleAsync(PickUpClaimRequest request, CancellationToken cancellationToken = default)
    {
        var claim = await claimRepository.GetByIdAsync(request.ClaimId, cancellationToken)
            ?? throw new KeyNotFoundException($"Claim '{request.ClaimId}' not found.");

        if (claim.Status is ClaimStatus.Settled or ClaimStatus.Rejected)
            throw new DomainException($"Cannot pick up a claim in terminal status '{claim.Status}'.");

        var nowUtc = DateTime.UtcNow;
        var officerId = request.OfficerId ?? "off-sarah";
        var officerName = request.OfficerName ?? "Admin Officer";

        claim.AssignedOfficerId = officerId;
        claim.AssignedOfficerName = officerName;

        if (claim.Status == ClaimStatus.Submitted)
            claim.Status = ClaimStatus.UnderReview;

        claim.AssessedReserve ??= claim.EstimatedLoss;
        claim.UpdatedAtUtc = nowUtc;

        claim.AuditTrail.Add(new ClaimAuditEntry
        {
            ClaimId = claim.Id,
            Action = "CLAIM_PICKED_UP",
            Description = $"Claim assigned to {officerName} and placed Under Review. Initial reserve set at {claim.Currency} {claim.ActiveReserveExposure:N2}.",
            ActorId = officerId,
            ActorName = officerName,
            ActorRole = "ClaimsOfficer",
            OccurredAtUtc = nowUtc
        });

        claim.DomainEvents.Add(new ClaimPickedUpDomainEvent(
            Guid.NewGuid(),
            nowUtc,
            claim.Id,
            claim.ClaimNumber,
            claim.Market,
            claim.ClaimType,
            claim.Status,
            claim.Currency,
            claim.ActiveReserveExposure,
            officerId,
            officerName));

        await claimRepository.UpdateAsync(claim, cancellationToken);
        await claimRepository.SaveAsync(cancellationToken);

        return ClaimMapper.ToDetailResponse(claim, fxRateService, includeInternalNotes: true);
    }
}
