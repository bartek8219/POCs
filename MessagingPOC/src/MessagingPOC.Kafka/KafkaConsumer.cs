using Confluent.Kafka;
using System.Text.Json;
using MessagingPOC.Abstractions;
using System.Threading.Tasks;

namespace MessagingPOC.Kafka;

public class KafkaConsumer : IMessageConsumer
{
    private readonly KafkaConfiguration _config;
    private IConsumer<string, string>? _consumer;
    private CancellationTokenSource? _cancellationTokenSource;

    public KafkaConsumer(KafkaConfiguration config)
    {
        _config = config;
    }

    public Task StartConsumingAsync<T>(string topic, Func<T, Task> messageHandler, CancellationToken cancellationToken = default)
    {
        return StartConsumingAsync(topic, Guid.NewGuid().ToString(), messageHandler, cancellationToken);
    }

    public Task StartConsumingAsync<T>(string topic, string consumerGroup, Func<T, Task> messageHandler, CancellationToken cancellationToken = default)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _config.BootstrapServers,
            GroupId = consumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false, // Manual commit dla większej kontroli
            EnableAutoOffsetStore = false
        };

        _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        _consumer.Subscribe(topic);

        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Uruchamiamy konsumpcję w osobnym Task
        _ = Task.Run(() => ConsumeLoopAsync<T>(messageHandler, _cancellationTokenSource.Token), _cancellationTokenSource.Token);

        return Task.CompletedTask;
    }
    
    private async Task ConsumeLoopAsync<T>(Func<T, Task> messageHandler, CancellationToken cancellationToken)
    {
        try
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(_cancellationTokenSource.Token);
                        
                    if (consumeResult?.Message?.Value != null)
                    {
                        var envelope = JsonSerializer.Deserialize<MessageEnvelope<T>>(consumeResult.Message.Value);
                            
                        if (envelope != null)
                        {
                            var payload = envelope.Payload;
                            // Dla wartościowych typów, 'payload' nigdy nie będzie null
                            // Dla referencyjnych można dodać warunek gdy T : class
                            if (payload != null || !typeof(T).IsValueType)
                            {
                                await messageHandler(payload!);
                                _consumer.Commit(consumeResult);
                            }
                        }
                    }
                }
                catch (ConsumeException ex)
                {
                    Console.WriteLine($"Kafka consume error: {ex.Error.Reason}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing message: {ex.Message}");
                    // W Kafka nie ma mechanizmu requeue - konsument musi ponownie przeczytać od ostatniego committed offset
                }
            }
        }
        finally
        {
            _consumer.Close();
        }
    }

    public Task StopConsumingAsync()
    {
        _cancellationTokenSource?.Cancel();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _consumer?.Close();
        _consumer?.Dispose();
        _cancellationTokenSource?.Dispose();
    }
}
