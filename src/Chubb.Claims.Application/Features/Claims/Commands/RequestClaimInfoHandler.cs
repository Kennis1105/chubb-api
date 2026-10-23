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

public sealed class RequestClaimInfoHandler(IClaimRepository claimRepository, IFxRateService fxRateService)
    : ICommandHandler<RequestInfoRequest, ClaimDetailResponse>
{
    public async Task<ClaimDetailResponse> HandleAsync(RequestInfoRequest request, CancellationToken cancellationToken = default)
    {
        var claim = await claimRepository.GetByIdAsync(request.ClaimId, cancellationToken)
            ?? throw new KeyNotFoundException($"Claim '{request.ClaimId}' not found.");

        if (claim.Status != ClaimStatus.UnderReview && claim.Status != ClaimStatus.Submitted)
            throw new DomainException($"Additional information can only be requested when a claim is Submitted or UnderReview (current: {claim.Status}).");

        if (string.IsNullOrWhiteSpace(request.Question))
            throw new DomainException("Question for the claimant cannot be empty.");

        var nowUtc = DateTime.UtcNow;
        var officerId = request.OfficerId ?? "off-sarah";
        var officerName = request.OfficerName ?? "Admin Officer";

        if (string.IsNullOrWhiteSpace(claim.AssignedOfficerId))
        {
            claim.AssignedOfficerId = officerId;
            claim.AssignedOfficerName = officerName;
        }

        claim.AssessedReserve ??= claim.EstimatedLoss;
        claim.Status = ClaimStatus.InfoRequested;
        claim.UpdatedAtUtc = nowUtc;

        claim.InformationRequests.Add(new ClaimInformationRequest
        {
            ClaimId = claim.Id,
            Question = request.Question.Trim(),
            RequestedByOfficerId = officerId,
            RequestedByOfficerName = officerName,
            RequestedAtUtc = nowUtc
        });

        claim.AuditTrail.Add(new ClaimAuditEntry
        {
            ClaimId = claim.Id,
            Action = "INFO_REQUESTED",
            Description = $"Requested additional information from claimant: \"{request.Question.Trim()}\"",
            ActorId = officerId,
            ActorName = officerName,
            ActorRole = "ClaimsOfficer",
            OccurredAtUtc = nowUtc
        });

        claim.DomainEvents.Add(new ClaimInfoRequestedDomainEvent(
            Guid.NewGuid(),
            nowUtc,
            claim.Id,
            claim.ClaimNumber,
            claim.Market,
            claim.ClaimType,
            claim.Status,
            claim.Currency,
            claim.ActiveReserveExposure,
            request.Question.Trim(),
            officerName));

        await claimRepository.UpdateAsync(claim, cancellationToken);
        await claimRepository.SaveAsync(cancellationToken);

        return ClaimMapper.ToDetailResponse(claim, fxRateService, includeInternalNotes: true);
    }
}
