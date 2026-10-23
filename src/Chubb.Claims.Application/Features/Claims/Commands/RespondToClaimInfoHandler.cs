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

public sealed class RespondToClaimInfoHandler(IClaimRepository claimRepository, IFxRateService fxRateService)
    : ICommandHandler<RespondInfoRequest, ClaimDetailResponse>
{
    public async Task<ClaimDetailResponse> HandleAsync(RespondInfoRequest request, CancellationToken cancellationToken = default)
    {
        var claim = await claimRepository.GetByIdAsync(request.ClaimId, cancellationToken)
            ?? throw new KeyNotFoundException($"Claim '{request.ClaimId}' not found.");

        if (claim.Status != ClaimStatus.InfoRequested)
            throw new DomainException($"Claim is not awaiting information (current status: {claim.Status}).");

        if (string.IsNullOrWhiteSpace(request.Response))
            throw new DomainException("Response cannot be empty.");

        var openRequest = claim.InformationRequests.LastOrDefault(r => !r.IsResolved)
            ?? throw new DomainException("No open information request found for this claim.");

        var nowUtc = DateTime.UtcNow;
        var claimantId = request.ClaimantId ?? "clm-tan";
        var claimantName = request.ClaimantName ?? "Lim Hean Soon";

        openRequest.ClaimantResponse = request.Response.Trim();
        openRequest.SupportingDocumentRef = request.SupportingDocumentRef?.Trim();
        openRequest.RespondedAtUtc = nowUtc;

        claim.Status = ClaimStatus.UnderReview;
        claim.UpdatedAtUtc = nowUtc;

        claim.AuditTrail.Add(new ClaimAuditEntry
        {
            ClaimId = claim.Id,
            Action = "INFO_RESPONDED",
            Description = $"Claimant provided requested information: \"{request.Response.Trim()}\"",
            ActorId = claimantId,
            ActorName = claimantName,
            ActorRole = "Claimant",
            OccurredAtUtc = nowUtc
        });

        claim.DomainEvents.Add(new ClaimInfoRespondedDomainEvent(
            Guid.NewGuid(),
            nowUtc,
            claim.Id,
            claim.ClaimNumber,
            claim.Market,
            claim.ClaimType,
            claim.Status,
            claim.Currency,
            claim.ActiveReserveExposure,
            request.Response.Trim(),
            claimantName));

        await claimRepository.UpdateAsync(claim, cancellationToken);
        await claimRepository.SaveAsync(cancellationToken);

        return ClaimMapper.ToDetailResponse(claim, fxRateService, includeInternalNotes: false);
    }
}
