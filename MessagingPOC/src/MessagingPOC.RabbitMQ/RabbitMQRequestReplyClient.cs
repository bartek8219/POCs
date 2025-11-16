using RabbitMQ.Client;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using MessagingPOC.Abstractions;

namespace MessagingPOC.RabbitMQ;

public class RabbitMQRequestReplyClient : IRequestReplyClient, IAsyncDisposable
{
    private IConnection? _connection;
    private IChannel? _channel;
    private string? _replyQueueName;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingRequests;
    private CancellationTokenSource? _cancellationTokenSource;

    public RabbitMQRequestReplyClient(RabbitMQConfiguration config)
    {
        _pendingRequests = new ConcurrentDictionary<string, TaskCompletionSource<string>>();
        Initialize(config).GetAwaiter().GetResult();
    }

    private async Task Initialize(RabbitMQConfiguration config)
    {
        var factory = new ConnectionFactory
        {
            HostName = config.HostName,
            Port = config.Port,
            UserName = config.UserName,
            Password = config.Password,
            VirtualHost = config.VirtualHost
        };

        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        // Tworzymy prywatną kolejkę do odpowiedzi
        var queueDeclareOk = await _channel.QueueDeclareAsync(
            queue: "",
            durable: false,
            exclusive: true,
            autoDelete: true);
        
        _replyQueueName = queueDeclareOk.QueueName;

        // Uruchamiamy listener dla odpowiedzi
        _cancellationTokenSource = new CancellationTokenSource();
        _ = Task.Run(() => ListenForRepliesAsync(_cancellationTokenSource.Token));
    }

    private async Task ListenForRepliesAsync(CancellationToken cancellationToken)
    {
        if (_channel == null)
            return;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var basicGetResult = await _channel.BasicGetAsync(_replyQueueName, autoAck: true, cancellationToken);

                    if (basicGetResult != null)
                    {
                        try
                        {
                            // CorrelationId jest już stringiem, nie byte[]
                            var correlationId = basicGetResult.BasicProperties?.CorrelationId;
                            
                            if (!string.IsNullOrEmpty(correlationId) && _pendingRequests.TryRemove(correlationId, out var tcs))
                            {
                                var body = basicGetResult.Body.ToArray();
                                var response = Encoding.UTF8.GetString(body);
                                tcs.SetResult(response);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing reply: {ex.Message}");
                        }
                    }
                    else
                    {
                        // Czekaj jeśli brak wiadomości
                        await Task.Delay(50, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error listening for replies: {ex.Message}");
                    await Task.Delay(500, cancellationToken);
                }
            }
        }
        finally
        {
            Console.WriteLine("Reply listener stopped");
        }
    }

    public async Task<TResponse> SendRequestAsync<TRequest, TResponse>(
        string requestTopic, 
        TRequest request, 
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (_channel == null)
            throw new InvalidOperationException("Channel not initialized");

        var correlationId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<string>();
        _pendingRequests[correlationId] = tcs;

        try
        {
            // Deklaracja request queue
            await _channel.QueueDeclareAsync(
                queue: requestTopic,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: cancellationToken);

            var envelope = new MessageEnvelope<TRequest>
            {
                CorrelationId = correlationId,
                Payload = request
            };

            var bodyJson = JsonSerializer.Serialize(envelope);
            var body = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(bodyJson));

            // Ustawiamy ReplyTo i CorrelationId w BasicProperties zgodnie z dobrymi praktykami request-reply
            var properties = new BasicProperties
            {
                CorrelationId = correlationId,
                ReplyTo = _replyQueueName,
                Persistent = false // Odpowiedzi nie muszą być trwałe
            };

            await _channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: requestTopic,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            // Czekamy na odpowiedź z timeoutem
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            var responseJson = await tcs.Task.WaitAsync(cts.Token);
            var responseEnvelope = JsonSerializer.Deserialize<MessageEnvelope<TResponse>>(responseJson);
        
            if (responseEnvelope == null)
            {
                throw new InvalidOperationException("Invalid response");
            }
        
            return responseEnvelope.Payload;
        }
        finally
        {
            _pendingRequests.TryRemove(correlationId, out _);
        }
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
