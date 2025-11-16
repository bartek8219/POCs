using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using MessagingPOC.Abstractions;

namespace MessagingPOC.RabbitMQ;

public class RabbitMQProducer : IMessageProducer
{
    private IConnection _connection;
    private IChannel _channel;
    private readonly RabbitMQConfiguration _config;

    public RabbitMQProducer(RabbitMQConfiguration config)
    {
        _config = config;
        Initialize().GetAwaiter().GetResult();
    }

    private async Task Initialize()
    {
        var factory = new ConnectionFactory
        {
            HostName = _config.HostName,
            Port = _config.Port,
            UserName = _config.UserName,
            Password = _config.Password,
            VirtualHost = _config.VirtualHost
        };

        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();
    }

    public Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default)
    {
        return PublishAsync(topic, string.Empty, message, cancellationToken);
    }

    public async Task PublishAsync<T>(string topic, string routingKey, T message, CancellationToken cancellationToken = default)
    {
        // Deklaracja fanout exchange dla wzorca fan-out
        await _channel.ExchangeDeclareAsync(
            exchange: topic,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        var envelope = new MessageEnvelope<T>
        {
            Payload = message
        };

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope));

        var properties = new BasicProperties
        {
            Persistent = true,
            MessageId = envelope.MessageId,
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };

        await _channel.BasicPublishAsync(
            exchange: topic,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }

    public async Task PublishBatchAsync<T>(string topic, IEnumerable<T> messages, CancellationToken cancellationToken = default)
    {
        foreach (var message in messages)
        {
            await PublishAsync(topic, message, cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel != null)
        {
            await _channel.CloseAsync();
            await _channel.DisposeAsync();
        }

        if (_connection != null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }
    }

    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }
}