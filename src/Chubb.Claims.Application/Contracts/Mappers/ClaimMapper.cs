using Chubb.Claims.Application.Common.Interfaces;
using Chubb.Claims.Application.Contracts.Responses;
using Chubb.Claims.Domain.Entities;
using Chubb.Claims.Domain.Enums;

namespace Chubb.Claims.Application.Contracts.Mappers;

public static class ClaimMapper
{
    public static ClaimSummaryResponse ToSummaryResponse(Claim claim, IFxRateService fx)
    {
        return new ClaimSummaryResponse(
            claim.Id,
            claim.ClaimNumber,
            claim.PolicyNumber,
            claim.Market.ToString(),
            MarketMetadata.GetCountryName(claim.Market),
            claim.ClaimType.ToString(),
            claim.Status.ToString(),
            claim.ClaimantId,
            claim.ClaimantName,
            claim.IncidentDateUtc,
            claim.IncidentLocation,
            claim.AssetIdentifier,
            claim.Currency,
            claim.EstimatedLoss,
            claim.AssessedReserve,
            claim.SettledAmount,
            claim.ActiveReserveExposure,
            fx.ConvertToUsd(claim.ActiveReserveExposure, claim.Currency),
            claim.AssignedOfficerId,
            claim.AssignedOfficerName,
            claim.InformationRequests.Any(r => !r.IsResolved),
            claim.SubmittedAtUtc,
            claim.UpdatedAtUtc);
    }

    public static ClaimDetailResponse ToDetailResponse(Claim claim, IFxRateService fx, bool includeInternalNotes)
    {
        var fxRate = fx.GetUsdRate(claim.Currency);
        var notes = claim.Notes
            .Where(n => includeInternalNotes || !n.IsInternalOnly)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Select(n => new ClaimNoteResponse(n.Id, n.AuthorId, n.AuthorName, n.Content, n.IsInternalOnly, n.CreatedAtUtc))
            .ToList();

        var infoRequests = claim.InformationRequests
            .OrderByDescending(r => r.RequestedAtUtc)
            .Select(r => new ClaimInformationRequestResponse(
                r.Id, r.Question, r.RequestedByOfficerId, r.RequestedByOfficerName,
                r.RequestedAtUtc, r.ClaimantResponse, r.SupportingDocumentRef, r.RespondedAtUtc, r.IsResolved))
            .ToList();

        var auditTrail = claim.AuditTrail
            .Where(a => includeInternalNotes || a.Action != "ASSESSMENT_NOTE_ADDED")
            .OrderByDescending(a => a.OccurredAtUtc)
            .Select(a => new ClaimAuditEntryResponse(a.Id, a.Action, a.Description, a.ActorId, a.ActorName, a.ActorRole, a.OccurredAtUtc))
            .ToList();

        return new ClaimDetailResponse(
            claim.Id,
            claim.ClaimNumber,
            claim.PolicyNumber,
            claim.Market.ToString(),
            MarketMetadata.GetCountryName(claim.Market),
            claim.ClaimType.ToString(),
            claim.Status.ToString(),
            claim.ClaimantId,
            claim.ClaimantName,
            claim.ClaimantEmail,
            claim.ClaimantPhone,
            claim.IncidentDateUtc,
            claim.IncidentLocation,
            claim.IncidentDescription,
            claim.AssetIdentifier,
            claim.Currency,
            fxRate,
            claim.EstimatedLoss,
            claim.AssessedReserve,
            claim.SettledAmount,
            claim.ActiveReserveExposure,
            fx.ConvertToUsd(claim.ActiveReserveExposure, claim.Currency),
            fx.ConvertToUsd(claim.RealizedPayout, claim.Currency),
            fx.ConvertToUsd(claim.ReleasedReserve, claim.Currency),
            claim.AssignedOfficerId,
            claim.AssignedOfficerName,
            claim.DecisionNotes,
            claim.SubmittedAtUtc,
            claim.UpdatedAtUtc,
            claim.ClosedAtUtc,
            infoRequests,
            notes,
            auditTrail);
    }
}
