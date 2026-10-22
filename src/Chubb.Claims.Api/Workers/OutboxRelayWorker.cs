using Chubb.Claims.Api.Hubs;
using Chubb.Claims.Infrastructure.Messaging;
using Chubb.Claims.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Chubb.Claims.Api.Workers;

/// <summary>
/// Background worker implementing the Transactional Outbox Pattern.
/// Polls unprocessed OutboxMessages persisted atomically with Claim state changes,
/// publishes them to Kafka (or In-Memory Channel fallback), marks them processed,
/// and pushes real-time events to all connected Angular clients over SignalR.
/// </summary>
public sealed class OutboxRelayWorker(
    IServiceScopeFactory scopeFactory,
    IKafkaHybridEventProducer eventProducer,
    IHubContext<ClaimsHub> hubContext,
    ILogger<OutboxRelayWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("OutboxRelayWorker started — polling OutboxMessages for Kafka/SignalR dispatch.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ClaimsDbContext>();

                var pendingMessages = await dbContext.OutboxMessages
                    .Where(m => m.ProcessedAtUtc == null)
                    .OrderBy(m => m.OccurredAtUtc)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                if (pendingMessages.Count > 0)
                {
                    foreach (var message in pendingMessages)
                    {
                        var transport = await eventProducer.PublishAsync(
                            message.AggregateId,
                            message.Topic,
                            message.EventType,
                            message.PayloadJson,
                            stoppingToken);

                        message.ProcessedAtUtc = DateTime.UtcNow;
                        message.PublishTransport = transport;

                        await hubContext.Clients.All.SendAsync("claimDomainEvent", new
                        {
                            eventId = message.Id,
                            claimId = message.AggregateId,
                            eventType = message.EventType,
                            topic = message.Topic,
                            transportMode = transport,
                            occurredAtUtc = message.OccurredAtUtc,
                            processedAtUtc = message.ProcessedAtUtc,
                            payloadJson = message.PayloadJson
                        }, stoppingToken);
                    }

                    await dbContext.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error occurred while relaying OutboxMessages.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(800), stoppingToken);
        }
    }
}
