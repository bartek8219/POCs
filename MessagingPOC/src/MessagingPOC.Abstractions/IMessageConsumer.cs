namespace MessagingPOC.Abstractions;

public interface IMessageConsumer : IDisposable
{
    /// <summary>
    /// Rozpoczyna konsumpcję wiadomości z określonego tematu/kolejki
    /// </summary>
    Task StartConsumingAsync<T>(string topic, Func<T, Task> messageHandler, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Rozpoczyna konsumpcję z wieloma konsumentami (dla fan-out)
    /// </summary>
    Task StartConsumingAsync<T>(string topic, string consumerGroup, Func<T, Task> messageHandler, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Zatrzymuje konsumpcję
    /// </summary>
    Task StopConsumingAsync();
}