using Confluent.Kafka;
using System.Collections.Concurrent;
using System.Text.Json;
using MessagingPOC.Abstractions;

namespace MessagingPOC.Kafka;

public class KafkaRequestReplyClient : IRequestReplyClient
{
    private readonly IProducer<string, string> _producer;
    private readonly IConsumer<string, string> _consumer;
    private readonly string _replyTopic;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingRequests;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public KafkaRequestReplyClient(KafkaConfiguration config)
    {
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = config.BootstrapServers,
            Acks = Acks.All
        };

        _producer = new ProducerBuilder<string, string>(producerConfig).Build();

        _replyTopic = $"reply-{Guid.NewGuid()}";
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = config.BootstrapServers,
            GroupId = _replyTopic,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        _consumer.Subscribe(_replyTopic);
        
        _pendingRequests = new ConcurrentDictionary<string, TaskCompletionSource<string>>();
        _cancellationTokenSource = new CancellationTokenSource();

        // Uruchamiamy listener dla odpowiedzi
        _ = Task.Run(() => ListenForReplies(_cancellationTokenSource.Token));
    }

    private async Task ListenForReplies(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var consumeResult = _consumer.Consume(cancellationToken);
                
                if (consumeResult?.Message?.Headers != null)
                {
                    var correlationIdBytes = consumeResult.Message.Headers
                        .FirstOrDefault(h => h.Key == "CorrelationId")?.GetValueBytes();
                    
                    if (correlationIdBytes != null)
                    {
                        var correlationId = System.Text.Encoding.UTF8.GetString(correlationIdBytes);
                        
                        if (_pendingRequests.TryRemove(correlationId, out var tcs))
                        {
                            tcs.SetResult(consumeResult.Message.Value);
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelling
        }
    }

    public async Task<TResponse> SendRequestAsync<TRequest, TResponse>(
        string requestTopic, 
        TRequest request, 
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<string>();
        _pendingRequests[correlationId] = tcs;

        try
        {
            var envelope = new MessageEnvelope<TRequest>
            {
                CorrelationId = correlationId,
                Payload = request
            };

            var message = new Message<string, string>
            {
                Value = JsonSerializer.Serialize(envelope),
                Headers = new Headers
                {
                    { "CorrelationId", System.Text.Encoding.UTF8.GetBytes(correlationId) },
                    { "ReplyTo", System.Text.Encoding.UTF8.GetBytes(_replyTopic) }
                }
            };

            await _producer.ProduceAsync(requestTopic, message, cancellationToken);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            var responseJson = await tcs.Task.WaitAsync(cts.Token);
            var responseEnvelope = JsonSerializer.Deserialize<MessageEnvelope<TResponse>>(responseJson);
            
            return responseEnvelope != null
                ? responseEnvelope.Payload
                : throw new InvalidOperationException("Invalid response");
        }
        finally
        {
            _pendingRequests.TryRemove(correlationId, out _);
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _producer?.Flush(TimeSpan.FromSeconds(5));
        _producer?.Dispose();
        _consumer?.Close();
        _consumer?.Dispose();
        _cancellationTokenSource?.Dispose();
    }
}
