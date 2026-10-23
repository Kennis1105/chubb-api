using Chubb.Claims.Domain.Entities;
using Chubb.Claims.Domain.Enums;

namespace Chubb.Claims.Application.Common.Interfaces;

public interface IClaimRepository
{
    Task<Claim?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Claim>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);
    Task<int> GetNextSequenceNumberAsync(MarketCode market, CancellationToken cancellationToken = default);
    Task AddAsync(Claim claim, CancellationToken cancellationToken = default);
    Task UpdateAsync(Claim claim, CancellationToken cancellationToken = default);
    Task SaveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OutboxMessage>> GetRecentOutboxMessagesAsync(int count = 25, CancellationToken cancellationToken = default);
}
