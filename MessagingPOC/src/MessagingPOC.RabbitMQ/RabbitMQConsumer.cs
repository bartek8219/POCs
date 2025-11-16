using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using MessagingPOC.Abstractions;

namespace MessagingPOC.RabbitMQ;

public class RabbitMQConsumer : IMessageConsumer, IAsyncDisposable
{
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly RabbitMQConfiguration _config;
    private CancellationTokenSource? _cancellationTokenSource;

    public RabbitMQConsumer(RabbitMQConfiguration config)
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

    public Task StartConsumingAsync<T>(string topic, Func<T, Task> messageHandler, CancellationToken cancellationToken = default)
    {
        return StartConsumingAsync(topic, Guid.NewGuid().ToString(), messageHandler, cancellationToken);
    }

    public async Task StartConsumingAsync<T>(string topic, string consumerGroup, Func<T, Task> messageHandler, CancellationToken cancellationToken = default)
    {
        if (_channel == null)
            throw new InvalidOperationException("Channel not initialized");

        // Deklaracja exchange
        await _channel.ExchangeDeclareAsync(
            exchange: topic,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        // Deklaracja kolejki dla consumer group
        //var queueName = $"{topic}.{consumerGroup}";
        var queueName = $"{topic}";
        await _channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        // Bind queue do exchange
        await _channel.QueueBindAsync(
            queue: queueName,
            exchange: topic,
            routingKey: string.Empty,
            cancellationToken: cancellationToken);

        // Ustawienie prefetch dla load balancing
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken);

        // Uruchamiamy konsumpcję w osobnym Task
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _ = Task.Run(() => ConsumeLoopAsync(queueName, messageHandler, _cancellationTokenSource.Token), cancellationToken);
    }

    private async Task ConsumeLoopAsync<T>(string queueName, Func<T, Task> messageHandler, CancellationToken cancellationToken)
    {
        if (_channel == null)
            throw new InvalidOperationException("Channel not initialized");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var basicGetResult = await _channel.BasicGetAsync(queueName, autoAck: false, cancellationToken);

                    if (basicGetResult != null)
                    {
                        try
                        {
                            var body = basicGetResult.Body.ToArray();
                            var json = Encoding.UTF8.GetString(body);
                            var envelope = JsonSerializer.Deserialize<MessageEnvelope<T>>(json);

                            if (envelope != null)
                            {
                                // Przekazujemy ReplyTo i CorrelationId z BasicProperties do MessageEnvelope.Headers
                                // aby były dostępne w handlerze (dla request-reply pattern)
                                if (basicGetResult.BasicProperties != null)
                                {
                                    if (!string.IsNullOrEmpty(basicGetResult.BasicProperties.ReplyTo))
                                    {
                                        envelope.Headers["ReplyTo"] = basicGetResult.BasicProperties.ReplyTo;
                                    }
                                    
                                    if (!string.IsNullOrEmpty(basicGetResult.BasicProperties.CorrelationId))
                                    {
                                        envelope.Headers["CorrelationId"] = basicGetResult.BasicProperties.CorrelationId;
                                        // Aktualizujemy też CorrelationId w envelope jeśli nie było ustawione
                                        if (string.IsNullOrEmpty(envelope.CorrelationId))
                                        {
                                            envelope.CorrelationId = basicGetResult.BasicProperties.CorrelationId;
                                        }
                                    }
                                }

                                await messageHandler(envelope.Payload);
                                await _channel.BasicAckAsync(basicGetResult.DeliveryTag, multiple: false, cancellationToken);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing message: {ex.Message}");
                            await _channel.BasicNackAsync(basicGetResult.DeliveryTag, multiple: false, requeue: true, cancellationToken);
                        }
                    }
                    else
                    {
                        // Czekaj jeśli brak wiadomości
                        await Task.Delay(100, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Consume error: {ex.Message}");
                    await Task.Delay(1000, cancellationToken);
                }
            }
        }
        finally
        {
            Console.WriteLine("Consumer loop stopped");
        }
    }

    public async Task StopConsumingAsync()
    {
        _cancellationTokenSource?.Cancel();
    }

    public async ValueTask DisposeAsync()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();

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
