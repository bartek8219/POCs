using Confluent.Kafka;
using System.Text.Json;
using MessagingPOC.Abstractions;

namespace MessagingPOC.Kafka;

public class KafkaProducer : IMessageProducer
{
    private readonly IProducer<string, string> _producer;
    private readonly KafkaConfiguration _config;

    public KafkaProducer(KafkaConfiguration config)
    {
        _config = config;
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = config.BootstrapServers,
            Acks = Acks.All, // Durability - czekamy na potwierdzenie od wszystkich replik
            EnableIdempotence = true,
            MessageTimeoutMs = 30000,
            RequestTimeoutMs = 30000
        };

        _producer = new ProducerBuilder<string, string>(producerConfig).Build();
    }

    public async Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default)
    {
        await PublishAsync(topic, string.Empty, message, cancellationToken);
    }

    public async Task PublishAsync<T>(string topic, string routingKey, T message, CancellationToken cancellationToken = default)
    {
        var envelope = new MessageEnvelope<T>
        {
            Payload = message
        };

        var kafkaMessage = new Message<string, string>
        {
            Key = string.IsNullOrEmpty(routingKey) ? null : routingKey,
            Value = JsonSerializer.Serialize(envelope),
            Headers = new Headers
            {
                { "MessageId", System.Text.Encoding.UTF8.GetBytes(envelope.MessageId) },
                { "MessageType", System.Text.Encoding.UTF8.GetBytes(envelope.MessageType) },
                { "Timestamp", System.Text.Encoding.UTF8.GetBytes(envelope.Timestamp.ToString("O")) }
            }
        };

        await _producer.ProduceAsync(topic, kafkaMessage, cancellationToken);
    }

    public async Task PublishBatchAsync<T>(string topic, IEnumerable<T> messages, CancellationToken cancellationToken = default)
    {
        var tasks = messages.Select(msg => PublishAsync(topic, msg, cancellationToken));
        await Task.WhenAll(tasks);
        _producer.Flush(cancellationToken);
    }

    public void Dispose()
    {
        _producer?.Flush(TimeSpan.FromSeconds(10));
        _producer?.Dispose();
    }
}
