using Chubb.Claims.Domain.Entities;
using Chubb.Claims.Domain.Enums;

namespace Chubb.Claims.Application.DTOs;

public sealed record ClaimInformationRequestDto(
    Guid Id,
    string Question,
    string RequestedByOfficerId,
    string RequestedByOfficerName,
    DateTime RequestedAtUtc,
    string? ClaimantResponse,
    string? SupportingDocumentRef,
    DateTime? RespondedAtUtc,
    bool IsResolved);

public sealed record ClaimNoteDto(
    Guid Id,
    string AuthorId,
    string AuthorName,
    string Content,
    bool IsInternalOnly,
    DateTime CreatedAtUtc);

public sealed record ClaimAuditEntryDto(
    Guid Id,
    string Action,
    string Description,
    string ActorId,
    string ActorName,
    string ActorRole,
    DateTime OccurredAtUtc);

public sealed record ClaimSummaryDto(
    Guid Id,
    string ClaimNumber,
    string PolicyNumber,
    string Market,
    string CountryName,
    string ClaimType,
    string Status,
    string ClaimantId,
    string ClaimantName,
    DateTime IncidentDateUtc,
    string IncidentLocation,
    string AssetIdentifier,
    string Currency,
    decimal EstimatedLoss,
    decimal? AssessedReserve,
    decimal? SettledAmount,
    decimal ActiveReserveExposure,
    decimal ActiveReserveExposureUsd,
    string? AssignedOfficerId,
    string? AssignedOfficerName,
    bool HasOpenInfoRequest,
    DateTime SubmittedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record ClaimDetailDto(
    Guid Id,
    string ClaimNumber,
    string PolicyNumber,
    string Market,
    string CountryName,
    string ClaimType,
    string Status,
    string ClaimantId,
    string ClaimantName,
    string ClaimantEmail,
    string ClaimantPhone,
    DateTime IncidentDateUtc,
    string IncidentLocation,
    string IncidentDescription,
    string AssetIdentifier,
    string Currency,
    decimal FxRateToUsd,
    decimal EstimatedLoss,
    decimal? AssessedReserve,
    decimal? SettledAmount,
    decimal ActiveReserveExposure,
    decimal ActiveReserveExposureUsd,
    decimal RealizedPayoutUsd,
    decimal ReleasedReserveUsd,
    string? AssignedOfficerId,
    string? AssignedOfficerName,
    string? DecisionNotes,
    DateTime SubmittedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? ClosedAtUtc,
    IReadOnlyList<ClaimInformationRequestDto> InformationRequests,
    IReadOnlyList<ClaimNoteDto> Notes,
    IReadOnlyList<ClaimAuditEntryDto> AuditTrail);

public sealed record MarketExposureBreakdownDto(
    string Market,
    string CountryName,
    string Currency,
    decimal FxRateToUsd,
    int TotalClaimsCount,
    int OpenClaimsCount,
    decimal ActiveReserveNative,
    decimal ActiveReserveUsd,
    decimal SettledPayoutNative,
    decimal SettledPayoutUsd);

public sealed record ClaimTypeBreakdownDto(
    string ClaimType,
    int OpenClaimsCount,
    int TotalClaimsCount,
    decimal ActiveReserveUsd,
    decimal SettledPayoutUsd);

public sealed record OfficerWorkloadDto(
    string OfficerId,
    string OfficerName,
    string PrimaryMarkets,
    int ActiveUnderReviewCount,
    int AwaitingInfoCount,
    int SettledCount,
    int RejectedCount,
    decimal ActiveReserveManagedUsd,
    double AverageResolutionHours);

public sealed record EventStreamItemDto(
    Guid EventId,
    Guid ClaimId,
    string EventType,
    string Topic,
    string TransportMode,
    DateTime OccurredAtUtc,
    DateTime? ProcessedAtUtc,
    string SummaryJson);

public sealed record ExposureAnalyticsDto(
    decimal TotalActiveLiabilityExposureUsd,
    decimal TotalSettledPayoutsUsd,
    decimal TotalReleasedReservesUsd,
    int TotalClaimsCount,
    int OpenClaimsCount,
    int UnassignedClaimsCount,
    int AwaitingClaimantInfoCount,
    int SettledClaimsCount,
    int RejectedClaimsCount,
    IReadOnlyList<MarketExposureBreakdownDto> ByMarket,
    IReadOnlyList<ClaimTypeBreakdownDto> ByClaimType,
    IReadOnlyList<OfficerWorkloadDto> TeamWorkload,
    IReadOnlyList<EventStreamItemDto> RecentEventStream);

public static class ClaimMapper
{
    public static ClaimSummaryDto ToSummaryDto(Claim claim, Chubb.Claims.Application.Common.Interfaces.IFxRateService fx)
    {
        return new ClaimSummaryDto(
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

    public static ClaimDetailDto ToDetailDto(Claim claim, Chubb.Claims.Application.Common.Interfaces.IFxRateService fx, bool includeInternalNotes)
    {
        var fxRate = fx.GetUsdRate(claim.Currency);
        var notes = claim.Notes
            .Where(n => includeInternalNotes || !n.IsInternalOnly)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Select(n => new ClaimNoteDto(n.Id, n.AuthorId, n.AuthorName, n.Content, n.IsInternalOnly, n.CreatedAtUtc))
            .ToList();

        var infoRequests = claim.InformationRequests
            .OrderByDescending(r => r.RequestedAtUtc)
            .Select(r => new ClaimInformationRequestDto(
                r.Id, r.Question, r.RequestedByOfficerId, r.RequestedByOfficerName,
                r.RequestedAtUtc, r.ClaimantResponse, r.SupportingDocumentRef, r.RespondedAtUtc, r.IsResolved))
            .ToList();

        var auditTrail = claim.AuditTrail
            .Where(a => includeInternalNotes || a.Action != "ASSESSMENT_NOTE_ADDED")
            .OrderByDescending(a => a.OccurredAtUtc)
            .Select(a => new ClaimAuditEntryDto(a.Id, a.Action, a.Description, a.ActorId, a.ActorName, a.ActorRole, a.OccurredAtUtc))
            .ToList();

        return new ClaimDetailDto(
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
