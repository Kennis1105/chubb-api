using Chubb.Claims.Domain.Common;

namespace Chubb.Claims.Domain.Entities;

public class OutboxMessage : IEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AggregateId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Topic { get; set; } = "chubb.apac.claims.events";
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAtUtc { get; set; }
    public string? PublishTransport { get; set; }
    public string? Error { get; set; }
}
