using Chubb.Claims.Domain.Common;

namespace Chubb.Claims.Domain.Entities;

public class ClaimInformationRequest : BaseEntity
{
    public Guid ClaimId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string RequestedByOfficerId { get; set; } = string.Empty;
    public string RequestedByOfficerName { get; set; } = string.Empty;
    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    public string? ClaimantResponse { get; set; }
    public string? SupportingDocumentRef { get; set; }
    public DateTime? RespondedAtUtc { get; set; }
    public bool IsResolved => RespondedAtUtc.HasValue;
}
