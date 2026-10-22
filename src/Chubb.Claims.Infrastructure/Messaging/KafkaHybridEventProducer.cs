using System.Collections.Concurrent;
using Chubb.Claims.Application.Common.Interfaces;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Chubb.Claims.Infrastructure.Messaging;

public interface IKafkaHybridEventProducer
{
    Task<string> PublishAsync(Guid claimId, string topic, string eventType, string payloadJson, CancellationToken cancellationToken = default);
}

public sealed class KafkaHybridEventProducer : IKafkaHybridEventProducer, IEventStreamBroadcaster, IDisposable
{
    private readonly ILogger<KafkaHybridEventProducer> _logger;
    private readonly IProducer<string, string>? _kafkaProducer;
    private readonly string? _bootstrapServers;
    private readonly ConcurrentQueue<IntegrationEventEnvelope> _recentEvents = new();

    public KafkaHybridEventProducer(IConfiguration configuration, ILogger<KafkaHybridEventProducer> logger)
    {
        _logger = logger;
        _bootstrapServers = configuration["Kafka:BootstrapServers"];

        if (!string.IsNullOrWhiteSpace(_bootstrapServers))
        {
            try
            {
                var config = new ProducerConfig
                {
                    BootstrapServers = _bootstrapServers,
                    SocketTimeoutMs = 1500,
                    MessageTimeoutMs = 2000,
                    Acks = Acks.Leader
                };
                _kafkaProducer = new ProducerBuilder<string, string>(config).Build();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Kafka producer initialization failed; falling back to In-Memory Event Bus.");
            }
        }
    }

    public async Task<string> PublishAsync(Guid claimId, string topic, string eventType, string payloadJson, CancellationToken cancellationToken = default)
    {
        var transportMode = "InMemoryChannel+SignalR";

        if (_kafkaProducer is not null)
        {
            try
            {
                var result = await _kafkaProducer.ProduceAsync(
                    topic,
                    new Message<string, string>
                    {
                        Key = claimId.ToString(),
                        Value = payloadJson
                    },
                    cancellationToken);

                transportMode = $"Kafka ({_bootstrapServers} @ offset {result.Offset})";
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Kafka broker unreachable ({Message}). Event routed via In-Memory Event Stream + SignalR.", ex.Message);
                transportMode = "KafkaFallback->InMemory+SignalR";
            }
        }

        var envelope = new IntegrationEventEnvelope(
            Guid.NewGuid(),
            claimId,
            eventType,
            topic,
            payloadJson,
            DateTime.UtcNow,
            transportMode);

        await PublishToLocalStreamAsync(envelope, cancellationToken);
        return transportMode;
    }

    public Task PublishToLocalStreamAsync(IntegrationEventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        _recentEvents.Enqueue(envelope);
        while (_recentEvents.Count > 50 && _recentEvents.TryDequeue(out _)) { }
        return Task.CompletedTask;
    }

    public IReadOnlyList<IntegrationEventEnvelope> GetRecentPublishedEvents(int count = 30)
    {
        return _recentEvents.Reverse().Take(count).ToList();
    }

    public void Dispose()
    {
        _kafkaProducer?.Dispose();
    }
}
