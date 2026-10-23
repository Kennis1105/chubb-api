using Chubb.Claims.Domain.Common;

namespace Chubb.Claims.Domain.Entities;

public class ClaimAuditEntry : BaseEntity
{
    public Guid ClaimId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ActorId { get; set; } = string.Empty;
    public string ActorName { get; set; } = string.Empty;
    public string ActorRole { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
