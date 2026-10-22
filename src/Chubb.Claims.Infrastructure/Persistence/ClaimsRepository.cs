using Chubb.Claims.Application.Common.Interfaces;
using Chubb.Claims.Domain.Entities;
using Chubb.Claims.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Chubb.Claims.Infrastructure.Persistence;

public sealed class ClaimsRepository(ClaimsDbContext dbContext) : IClaimsRepository
{
    public async Task<Claim?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Claims
            .Include(c => c.InformationRequests)
            .Include(c => c.Notes)
            .Include(c => c.AuditTrail)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Claim>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Claims
            .Include(c => c.InformationRequests)
            .Include(c => c.Notes)
            .Include(c => c.AuditTrail)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Claim claim, CancellationToken cancellationToken = default)
    {
        await dbContext.Claims.AddAsync(claim, cancellationToken);
    }

    public async Task<int> GetNextSequenceNumberAsync(MarketCode market, CancellationToken cancellationToken = default)
    {
        var count = await dbContext.Claims.CountAsync(c => c.Market == market, cancellationToken);
        return 1001 + count;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetRecentOutboxMessagesAsync(int count = 25, CancellationToken cancellationToken = default)
    {
        return await dbContext.OutboxMessages
            .AsNoTracking()
            .OrderByDescending(m => m.OccurredAtUtc)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}
