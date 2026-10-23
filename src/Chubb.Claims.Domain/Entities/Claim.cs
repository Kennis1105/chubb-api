using Chubb.Claims.Domain.Common;
using Chubb.Claims.Domain.Enums;

namespace Chubb.Claims.Domain.Entities;

public class Claim : BaseEntity
{
    public string ClaimNumber { get; set; } = string.Empty;
    public string PolicyNumber { get; set; } = string.Empty;
    public MarketCode Market { get; set; }
    public ClaimType ClaimType { get; set; }
    public ClaimStatus Status { get; set; } = ClaimStatus.Submitted;

    public string ClaimantId { get; set; } = string.Empty;
    public string ClaimantName { get; set; } = string.Empty;
    public string ClaimantEmail { get; set; } = string.Empty;
    public string ClaimantPhone { get; set; } = string.Empty;

    public DateTime IncidentDateUtc { get; set; }
    public string IncidentLocation { get; set; } = string.Empty;
    public string IncidentDescription { get; set; } = string.Empty;
    public string AssetIdentifier { get; set; } = string.Empty;

    public string Currency { get; set; } = string.Empty;
    public decimal EstimatedLoss { get; set; }
    public decimal? AssessedReserve { get; set; }
    public decimal? SettledAmount { get; set; }

    public string? AssignedOfficerId { get; set; }
    public string? AssignedOfficerName { get; set; }

    public string? DecisionNotes { get; set; }
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAtUtc { get; set; }

    public List<ClaimInformationRequest> InformationRequests { get; set; } = new();
    public List<ClaimNote> Notes { get; set; } = new();
    public List<ClaimAuditEntry> AuditTrail { get; set; } = new();

    public decimal ActiveReserveExposure =>
        Status is ClaimStatus.Submitted or ClaimStatus.UnderReview or ClaimStatus.InfoRequested
            ? (AssessedReserve ?? EstimatedLoss)
            : 0m;

    public decimal RealizedPayout =>
        Status == ClaimStatus.Settled ? (SettledAmount ?? 0m) : 0m;

    public decimal ReleasedReserve =>
        Status == ClaimStatus.Rejected ? (AssessedReserve ?? EstimatedLoss) : 0m;
}
