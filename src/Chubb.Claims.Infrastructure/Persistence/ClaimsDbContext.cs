using System.Text.Json;
using Chubb.Claims.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Chubb.Claims.Infrastructure.Persistence;

public class ClaimsDbContext(DbContextOptions<ClaimsDbContext> options) : DbContext(options)
{
    public DbSet<Claim> Claims => Set<Claim>();
    public DbSet<ClaimInformationRequest> InformationRequests => Set<ClaimInformationRequest>();
    public DbSet<ClaimNote> Notes => Set<ClaimNote>();
    public DbSet<ClaimAuditEntry> AuditEntries => Set<ClaimAuditEntry>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Claim>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => c.ClaimNumber).IsUnique();
            entity.Property(c => c.Market).HasConversion<string>();
            entity.Property(c => c.ClaimType).HasConversion<string>();
            entity.Property(c => c.Status).HasConversion<string>();
            entity.Property(c => c.EstimatedLoss).HasPrecision(18, 2);
            entity.Property(c => c.AssessedReserve).HasPrecision(18, 2);
            entity.Property(c => c.SettledAmount).HasPrecision(18, 2);

            entity.Ignore(c => c.DomainEvents);
            entity.Ignore(c => c.ActiveReserveExposure);
            entity.Ignore(c => c.RealizedPayout);
            entity.Ignore(c => c.ReleasedReserve);

            entity.HasMany(c => c.InformationRequests)
                .WithOne()
                .HasForeignKey(r => r.ClaimId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(c => c.Notes)
                .WithOne()
                .HasForeignKey(n => n.ClaimId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(c => c.AuditTrail)
                .WithOne()
                .HasForeignKey(a => a.ClaimId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(c => c.InformationRequests).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.Navigation(c => c.Notes).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.Navigation(c => c.AuditTrail).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<ClaimInformationRequest>(entity =>
        {
            entity.HasKey(r => r.Id);
        });

        modelBuilder.Entity<ClaimNote>(entity =>
        {
            entity.HasKey(n => n.Id);
        });

        modelBuilder.Entity<ClaimAuditEntry>(entity =>
        {
            entity.HasKey(a => a.Id);
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.HasIndex(o => o.ProcessedAtUtc);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Ensure child entities added to private backing lists on tracked Claims are explicitly tracked as Added
        foreach (var claimEntry in ChangeTracker.Entries<Claim>())
        {
            foreach (var req in claimEntry.Entity.InformationRequests)
            {
                if (Entry(req).State == EntityState.Detached || (Entry(req).State == EntityState.Modified && !await InformationRequests.AnyAsync(x => x.Id == req.Id, cancellationToken)))
                {
                    Entry(req).State = EntityState.Added;
                }
            }

            foreach (var note in claimEntry.Entity.Notes)
            {
                if (Entry(note).State == EntityState.Detached || (Entry(note).State == EntityState.Modified && !await Notes.AnyAsync(x => x.Id == note.Id, cancellationToken)))
                {
                    Entry(note).State = EntityState.Added;
                }
            }

            foreach (var audit in claimEntry.Entity.AuditTrail)
            {
                if (Entry(audit).State == EntityState.Detached || (Entry(audit).State == EntityState.Modified && !await AuditEntries.AnyAsync(x => x.Id == audit.Id, cancellationToken)))
                {
                    Entry(audit).State = EntityState.Added;
                }
            }
        }

        // Extract Domain Events and persist as Transactional Outbox messages in the same atomic DB transaction
        var claimsWithEvents = ChangeTracker.Entries<Claim>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        foreach (var claim in claimsWithEvents)
        {
            foreach (var domainEvent in claim.DomainEvents)
            {
                var payloadJson = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                OutboxMessages.Add(new OutboxMessage
                {
                    Id = domainEvent.EventId,
                    AggregateId = claim.Id,
                    EventType = domainEvent.EventType,
                    Topic = "chubb.apac.claims.events",
                    PayloadJson = payloadJson,
                    OccurredAtUtc = domainEvent.OccurredAtUtc
                });
            }

            claim.ClearDomainEvents();
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
