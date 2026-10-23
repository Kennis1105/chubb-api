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

public sealed class UpdateClaimAssessmentHandler(IClaimRepository claimRepository, IFxRateService fxRateService)
    : ICommandHandler<UpdateAssessmentRequest, ClaimDetailResponse>
{
    public async Task<ClaimDetailResponse> HandleAsync(UpdateAssessmentRequest request, CancellationToken cancellationToken = default)
    {
        var claim = await claimRepository.GetByIdAsync(request.ClaimId, cancellationToken)
            ?? throw new KeyNotFoundException($"Claim '{request.ClaimId}' not found.");

        if (claim.Status is ClaimStatus.Settled or ClaimStatus.Rejected)
            throw new DomainException($"Cannot update assessment when claim is in terminal status '{claim.Status}'.");

        var nowUtc = DateTime.UtcNow;
        var officerId = request.OfficerId ?? "off-sarah";
        var officerName = request.OfficerName ?? "Admin Officer";

        if (string.IsNullOrWhiteSpace(claim.AssignedOfficerId))
        {
            claim.AssignedOfficerId = officerId;
            claim.AssignedOfficerName = officerName;
            if (claim.Status == ClaimStatus.Submitted)
                claim.Status = ClaimStatus.UnderReview;
        }

        var previousReserve = claim.ActiveReserveExposure;

        if (request.AssessedReserve.HasValue)
        {
            if (request.AssessedReserve.Value <= 0)
                throw new DomainException("Assessed reserve must be greater than zero.");

            claim.AssessedReserve = decimal.Round(request.AssessedReserve.Value, 2);
            claim.AuditTrail.Add(new ClaimAuditEntry
            {
                ClaimId = claim.Id,
                Action = "RESERVE_ADJUSTED",
                Description = $"Reserve adjusted from {claim.Currency} {previousReserve:N2} to {claim.Currency} {claim.AssessedReserve.Value:N2}.",
                ActorId = officerId,
                ActorName = officerName,
                ActorRole = "ClaimsOfficer",
                OccurredAtUtc = nowUtc
            });

            claim.DomainEvents.Add(new ClaimReserveUpdatedDomainEvent(
                Guid.NewGuid(),
                nowUtc,
                claim.Id,
                claim.ClaimNumber,
                claim.Market,
                claim.ClaimType,
                claim.Status,
                claim.Currency,
                previousReserve,
                claim.AssessedReserve.Value,
                claim.ActiveReserveExposure,
                officerName));
        }

        if (!string.IsNullOrWhiteSpace(request.InternalNote))
        {
            claim.Notes.Add(new ClaimNote
            {
                ClaimId = claim.Id,
                AuthorId = officerId,
                AuthorName = officerName,
                Content = request.InternalNote.Trim(),
                IsInternalOnly = true,
                CreatedAtUtc = nowUtc
            });

            claim.AuditTrail.Add(new ClaimAuditEntry
            {
                ClaimId = claim.Id,
                Action = "ASSESSMENT_NOTE_ADDED",
                Description = "Officer recorded an internal assessment note.",
                ActorId = officerId,
                ActorName = officerName,
                ActorRole = "ClaimsOfficer",
                OccurredAtUtc = nowUtc
            });
        }

        claim.UpdatedAtUtc = nowUtc;

        await claimRepository.UpdateAsync(claim, cancellationToken);
        await claimRepository.SaveAsync(cancellationToken);

        return ClaimMapper.ToDetailResponse(claim, fxRateService, includeInternalNotes: true);
    }
}
