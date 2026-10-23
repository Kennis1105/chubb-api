using Chubb.Claims.Domain.Common;

namespace Chubb.Claims.Domain.Entities;

public class ClaimNote : BaseEntity
{
    public Guid ClaimId { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsInternalOnly { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
