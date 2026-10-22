namespace Chubb.Claims.Domain.Entities;

public class ClaimInformationRequest
{
    public Guid Id { get; private set; }
    public Guid ClaimId { get; private set; }
    public string Question { get; private set; } = string.Empty;
    public string RequestedByOfficerId { get; private set; } = string.Empty;
    public string RequestedByOfficerName { get; private set; } = string.Empty;
    public DateTime RequestedAtUtc { get; private set; }
    public string? ClaimantResponse { get; private set; }
    public string? SupportingDocumentRef { get; private set; }
    public DateTime? RespondedAtUtc { get; private set; }
    public bool IsResolved => RespondedAtUtc.HasValue;

    private ClaimInformationRequest() { }

    public ClaimInformationRequest(Guid claimId, string question, string officerId, string officerName, DateTime requestedAtUtc)
    {
        Id = Guid.NewGuid();
        ClaimId = claimId;
        Question = question;
        RequestedByOfficerId = officerId;
        RequestedByOfficerName = officerName;
        RequestedAtUtc = requestedAtUtc;
    }

    public void ProvideResponse(string response, string? supportingDocRef, DateTime respondedAtUtc)
    {
        ClaimantResponse = response;
        SupportingDocumentRef = supportingDocRef;
        RespondedAtUtc = respondedAtUtc;
    }
}

public class ClaimNote
{
    public Guid Id { get; private set; }
    public Guid ClaimId { get; private set; }
    public string AuthorId { get; private set; } = string.Empty;
    public string AuthorName { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public bool IsInternalOnly { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private ClaimNote() { }

    public ClaimNote(Guid claimId, string authorId, string authorName, string content, bool isInternalOnly, DateTime createdAtUtc)
    {
        Id = Guid.NewGuid();
        ClaimId = claimId;
        AuthorId = authorId;
        AuthorName = authorName;
        Content = content;
        IsInternalOnly = isInternalOnly;
        CreatedAtUtc = createdAtUtc;
    }
}

public class ClaimAuditEntry
{
    public Guid Id { get; private set; }
    public Guid ClaimId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string ActorId { get; private set; } = string.Empty;
    public string ActorName { get; private set; } = string.Empty;
    public string ActorRole { get; private set; } = string.Empty;
    public DateTime OccurredAtUtc { get; private set; }

    private ClaimAuditEntry() { }

    public ClaimAuditEntry(Guid claimId, string action, string description, string actorId, string actorName, string actorRole, DateTime occurredAtUtc)
    {
        Id = Guid.NewGuid();
        ClaimId = claimId;
        Action = action;
        Description = description;
        ActorId = actorId;
        ActorName = actorName;
        ActorRole = actorRole;
        OccurredAtUtc = occurredAtUtc;
    }
}

public class OutboxMessage
{
    public Guid Id { get; set; }
    public Guid AggregateId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Topic { get; set; } = "chubb.apac.claims.events";
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public string? PublishTransport { get; set; }
    public string? Error { get; set; }
}
