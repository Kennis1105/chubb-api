namespace Chubb.Claims.Domain.Common;

public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredAtUtc { get; }
    string EventType { get; }
}

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
