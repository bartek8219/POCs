using MessagingPOC.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MessagingPOC.Consumer.Workers;

public class MessageWorker : BackgroundService
{
    private readonly IMessageConsumer _consumer;
    private readonly ILogger<MessageWorker> _logger;
    private readonly string _consumerGroup;

    public MessageWorker(IMessageConsumer consumer, ILogger<MessageWorker> logger)
    {
        _consumer = consumer;
        _logger = logger;
        _consumerGroup = $"consumer-group-{Environment.MachineName}";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation($"MessageWorker started in group: {_consumerGroup}");

        try
        {
            await _consumer.StartConsumingAsync<SampleMessage>(
                "fanout-topic",
                _consumerGroup,
                async message =>
                {
                    _logger.LogInformation(
                        $"[{_consumerGroup}] Received message: {message.Content} at {message.Timestamp}");
                    
                    // Symulacja przetwarzania
                    await Task.Delay(100, stoppingToken);
                },
                stoppingToken);

            // Czekamy na anulowanie
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("MessageWorker cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in MessageWorker");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MessageWorker stopping");
        await _consumer.StopConsumingAsync();
        await base.StopAsync(cancellationToken);
    }
}

public record SampleMessage
{
    public string Content { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}