namespace Chubb.Claims.Domain.Common;

public abstract class BaseEntity : IEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public List<IDomainEvent> DomainEvents { get; set; } = new();
}
