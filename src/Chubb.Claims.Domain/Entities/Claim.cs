using Chubb.Claims.Domain.Common;
using Chubb.Claims.Domain.Enums;
using Chubb.Claims.Domain.Events;

namespace Chubb.Claims.Domain.Entities;

public class Claim
{
    private readonly List<ClaimInformationRequest> _informationRequests = new();
    private readonly List<ClaimNote> _notes = new();
    private readonly List<ClaimAuditEntry> _auditTrail = new();
    private readonly List<IDomainEvent> _domainEvents = new();

    public Guid Id { get; private set; }
    public string ClaimNumber { get; private set; } = string.Empty;
    public string PolicyNumber { get; private set; } = string.Empty;
    public MarketCode Market { get; private set; }
    public ClaimType ClaimType { get; private set; }
    public ClaimStatus Status { get; private set; }

    public string ClaimantId { get; private set; } = string.Empty;
    public string ClaimantName { get; private set; } = string.Empty;
    public string ClaimantEmail { get; private set; } = string.Empty;
    public string ClaimantPhone { get; private set; } = string.Empty;

    public DateTime IncidentDateUtc { get; private set; }
    public string IncidentLocation { get; private set; } = string.Empty;
    public string IncidentDescription { get; private set; } = string.Empty;
    public string AssetIdentifier { get; private set; } = string.Empty;

    public string Currency { get; private set; } = string.Empty;
    public decimal EstimatedLoss { get; private set; }
    public decimal? AssessedReserve { get; private set; }
    public decimal? SettledAmount { get; private set; }

    public string? AssignedOfficerId { get; private set; }
    public string? AssignedOfficerName { get; private set; }

    public string? DecisionNotes { get; private set; }
    public DateTime SubmittedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }

    public IReadOnlyCollection<ClaimInformationRequest> InformationRequests => _informationRequests.AsReadOnly();
    public IReadOnlyCollection<ClaimNote> Notes => _notes.AsReadOnly();
    public IReadOnlyCollection<ClaimAuditEntry> AuditTrail => _auditTrail.AsReadOnly();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Outstanding Liability Exposure in native currency.
    /// Open claims contribute AssessedReserve (if assessed) or EstimatedLoss.
    /// Terminal claims (Settled/Rejected) have 0 active reserve exposure.
    /// </summary>
    public decimal ActiveReserveExposure =>
        Status is ClaimStatus.Submitted or ClaimStatus.UnderReview or ClaimStatus.InfoRequested
            ? (AssessedReserve ?? EstimatedLoss)
            : 0m;

    /// <summary>
    /// Realized payout for settled claims in native currency.
    /// </summary>
    public decimal RealizedPayout =>
        Status == ClaimStatus.Settled ? (SettledAmount ?? 0m) : 0m;

    /// <summary>
    /// Reserve amount released without payout upon rejection in native currency.
    /// </summary>
    public decimal ReleasedReserve =>
        Status == ClaimStatus.Rejected ? (AssessedReserve ?? EstimatedLoss) : 0m;

    private Claim() { }

    public static Claim Submit(
        string claimNumber,
        string policyNumber,
        MarketCode market,
        ClaimType claimType,
        string claimantId,
        string claimantName,
        string claimantEmail,
        string claimantPhone,
        DateTime incidentDateUtc,
        string incidentLocation,
        string incidentDescription,
        string assetIdentifier,
        decimal estimatedLoss,
        DateTime? nowUtc = null)
    {
        if (string.IsNullOrWhiteSpace(policyNumber))
            throw new DomainException("Policy number is required.");
        if (string.IsNullOrWhiteSpace(incidentDescription))
            throw new DomainException("Incident description is required.");
        if (estimatedLoss <= 0)
            throw new DomainException("Estimated loss must be greater than zero.");

        var timestamp = nowUtc ?? DateTime.UtcNow;
        var currency = MarketMetadata.GetCurrency(market);

        var claim = new Claim
        {
            Id = Guid.NewGuid(),
            ClaimNumber = claimNumber,
            PolicyNumber = policyNumber.Trim().ToUpperInvariant(),
            Market = market,
            ClaimType = claimType,
            Status = ClaimStatus.Submitted,
            ClaimantId = claimantId,
            ClaimantName = claimantName.Trim(),
            ClaimantEmail = claimantEmail.Trim(),
            ClaimantPhone = claimantPhone.Trim(),
            IncidentDateUtc = incidentDateUtc,
            IncidentLocation = incidentLocation.Trim(),
            IncidentDescription = incidentDescription.Trim(),
            AssetIdentifier = assetIdentifier.Trim(),
            Currency = currency,
            EstimatedLoss = decimal.Round(estimatedLoss, 2),
            SubmittedAtUtc = timestamp,
            UpdatedAtUtc = timestamp
        };

        claim.AddAudit("CLAIM_SUBMITTED",
            $"FNOL reported for {claimType} policy {claim.PolicyNumber} in {market} ({currency} {claim.EstimatedLoss:N2}).",
            claimantId, claimantName, "Claimant", timestamp);

        claim.RaiseEvent(new ClaimSubmittedDomainEvent(
            Guid.NewGuid(),
            timestamp,
            claim.Id,
            claim.ClaimNumber,
            claim.Market,
            claim.ClaimType,
            claim.Status,
            claim.Currency,
            claim.EstimatedLoss,
            claim.ActiveReserveExposure,
            claim.ClaimantName));

        return claim;
    }

    public void PickUp(string officerId, string officerName, DateTime? nowUtc = null)
    {
        if (Status is ClaimStatus.Settled or ClaimStatus.Rejected)
            throw new DomainException($"Cannot pick up a claim in terminal status '{Status}'.");

        var timestamp = nowUtc ?? DateTime.UtcNow;
        AssignedOfficerId = officerId;
        AssignedOfficerName = officerName;

        if (Status == ClaimStatus.Submitted)
        {
            Status = ClaimStatus.UnderReview;
        }

        AssessedReserve ??= EstimatedLoss;
        UpdatedAtUtc = timestamp;

        AddAudit("CLAIM_PICKED_UP",
            $"Claim assigned to {officerName} and placed Under Review. Initial reserve set at {Currency} {ActiveReserveExposure:N2}.",
            officerId, officerName, "ClaimsOfficer", timestamp);

        RaiseEvent(new ClaimPickedUpDomainEvent(
            Guid.NewGuid(),
            timestamp,
            Id,
            ClaimNumber,
            Market,
            ClaimType,
            Status,
            Currency,
            ActiveReserveExposure,
            officerId,
            officerName));
    }

    public void UpdateAssessment(decimal? newAssessedReserve, string? internalNote, string officerId, string officerName, DateTime? nowUtc = null)
    {
        EnsureNotTerminal("update assessment");

        var timestamp = nowUtc ?? DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(AssignedOfficerId))
        {
            AssignedOfficerId = officerId;
            AssignedOfficerName = officerName;
            if (Status == ClaimStatus.Submitted)
                Status = ClaimStatus.UnderReview;
        }

        var previousReserve = ActiveReserveExposure;

        if (newAssessedReserve.HasValue)
        {
            if (newAssessedReserve.Value <= 0)
                throw new DomainException("Assessed reserve must be greater than zero.");

            AssessedReserve = decimal.Round(newAssessedReserve.Value, 2);
            AddAudit("RESERVE_ADJUSTED",
                $"Reserve adjusted from {Currency} {previousReserve:N2} to {Currency} {AssessedReserve.Value:N2}.",
                officerId, officerName, "ClaimsOfficer", timestamp);

            RaiseEvent(new ClaimReserveUpdatedDomainEvent(
                Guid.NewGuid(),
                timestamp,
                Id,
                ClaimNumber,
                Market,
                ClaimType,
                Status,
                Currency,
                previousReserve,
                AssessedReserve.Value,
                ActiveReserveExposure,
                officerName));
        }

        if (!string.IsNullOrWhiteSpace(internalNote))
        {
            _notes.Add(new ClaimNote(Id, officerId, officerName, internalNote.Trim(), isInternalOnly: true, timestamp));
            AddAudit("ASSESSMENT_NOTE_ADDED",
                "Officer recorded an internal assessment note.",
                officerId, officerName, "ClaimsOfficer", timestamp);
        }

        UpdatedAtUtc = timestamp;
    }

    public ClaimInformationRequest RequestAdditionalInfo(string question, string officerId, string officerName, DateTime? nowUtc = null)
    {
        if (Status != ClaimStatus.UnderReview && Status != ClaimStatus.Submitted)
            throw new DomainException($"Additional information can only be requested when a claim is Submitted or UnderReview (current: {Status}).");

        if (string.IsNullOrWhiteSpace(question))
            throw new DomainException("Question for the claimant cannot be empty.");

        var timestamp = nowUtc ?? DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(AssignedOfficerId))
        {
            AssignedOfficerId = officerId;
            AssignedOfficerName = officerName;
        }

        AssessedReserve ??= EstimatedLoss;
        Status = ClaimStatus.InfoRequested;
        UpdatedAtUtc = timestamp;

        var infoRequest = new ClaimInformationRequest(Id, question.Trim(), officerId, officerName, timestamp);
        _informationRequests.Add(infoRequest);

        AddAudit("INFO_REQUESTED",
            $"Requested additional information from claimant: \"{question.Trim()}\"",
            officerId, officerName, "ClaimsOfficer", timestamp);

        RaiseEvent(new ClaimInfoRequestedDomainEvent(
            Guid.NewGuid(),
            timestamp,
            Id,
            ClaimNumber,
            Market,
            ClaimType,
            Status,
            Currency,
            ActiveReserveExposure,
            question.Trim(),
            officerName));

        return infoRequest;
    }

    public void RespondToInformationRequest(string response, string? supportingDocRef, string claimantId, string claimantName, DateTime? nowUtc = null)
    {
        if (Status != ClaimStatus.InfoRequested)
            throw new DomainException($"Claim is not awaiting information (current status: {Status}).");

        if (string.IsNullOrWhiteSpace(response))
            throw new DomainException("Response cannot be empty.");

        var timestamp = nowUtc ?? DateTime.UtcNow;
        var openRequest = _informationRequests.LastOrDefault(r => !r.IsResolved)
            ?? throw new DomainException("No open information request found for this claim.");

        openRequest.ProvideResponse(response.Trim(), supportingDocRef?.Trim(), timestamp);
        Status = ClaimStatus.UnderReview;
        UpdatedAtUtc = timestamp;

        AddAudit("INFO_RESPONDED",
            $"Claimant provided requested information: \"{response.Trim()}\"" +
            (!string.IsNullOrWhiteSpace(supportingDocRef) ? $" (Attachment: {supportingDocRef.Trim()})" : ""),
            claimantId, claimantName, "Claimant", timestamp);

        RaiseEvent(new ClaimInfoRespondedDomainEvent(
            Guid.NewGuid(),
            timestamp,
            Id,
            ClaimNumber,
            Market,
            ClaimType,
            Status,
            Currency,
            ActiveReserveExposure,
            response.Trim(),
            claimantName));
    }

    public void Settle(decimal settledAmount, string decisionNotes, string officerId, string officerName, DateTime? nowUtc = null)
    {
        if (Status != ClaimStatus.UnderReview)
            throw new DomainException($"Only claims in UnderReview status can be settled (current status: {Status}).");

        if (settledAmount <= 0)
            throw new DomainException("Settled amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(decisionNotes))
            throw new DomainException("Settlement decision notes are required.");

        var timestamp = nowUtc ?? DateTime.UtcNow;
        var previousReserve = ActiveReserveExposure;

        SettledAmount = decimal.Round(settledAmount, 2);
        DecisionNotes = decisionNotes.Trim();
        Status = ClaimStatus.Settled;
        UpdatedAtUtc = timestamp;
        ClosedAtUtc = timestamp;

        AddAudit("CLAIM_SETTLED",
            $"Claim approved and settled for {Currency} {SettledAmount.Value:N2}. Active reserve of {Currency} {previousReserve:N2} released. Decision: {DecisionNotes}",
            officerId, officerName, "ClaimsOfficer", timestamp);

        RaiseEvent(new ClaimSettledDomainEvent(
            Guid.NewGuid(),
            timestamp,
            Id,
            ClaimNumber,
            Market,
            ClaimType,
            Status,
            Currency,
            SettledAmount.Value,
            previousReserve,
            DecisionNotes,
            officerName));
    }

    public void Reject(string rejectionReason, string officerId, string officerName, DateTime? nowUtc = null)
    {
        if (Status != ClaimStatus.UnderReview)
            throw new DomainException($"Only claims in UnderReview status can be rejected (current status: {Status}).");

        if (string.IsNullOrWhiteSpace(rejectionReason))
            throw new DomainException("Rejection reason is required.");

        var timestamp = nowUtc ?? DateTime.UtcNow;
        var releasedReserve = ActiveReserveExposure;

        DecisionNotes = rejectionReason.Trim();
        Status = ClaimStatus.Rejected;
        UpdatedAtUtc = timestamp;
        ClosedAtUtc = timestamp;

        AddAudit("CLAIM_REJECTED",
            $"Claim rejected. Reserve of {Currency} {releasedReserve:N2} released. Reason: {DecisionNotes}",
            officerId, officerName, "ClaimsOfficer", timestamp);

        RaiseEvent(new ClaimRejectedDomainEvent(
            Guid.NewGuid(),
            timestamp,
            Id,
            ClaimNumber,
            Market,
            ClaimType,
            Status,
            Currency,
            releasedReserve,
            DecisionNotes,
            officerName));
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    private void EnsureNotTerminal(string operation)
    {
        if (Status is ClaimStatus.Settled or ClaimStatus.Rejected)
            throw new DomainException($"Cannot {operation} when claim is in terminal status '{Status}'.");
    }

    private void AddAudit(string action, string description, string actorId, string actorName, string actorRole, DateTime occurredAtUtc)
    {
        _auditTrail.Add(new ClaimAuditEntry(Id, action, description, actorId, actorName, actorRole, occurredAtUtc));
    }

    private void RaiseEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
}
