namespace MessagingPOC.Abstractions;

public interface IMessageProducer : IDisposable
{
    /// <summary>
    /// Publikuje wiadomość do określonego tematu/kolejki
    /// </summary>
    Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Publikuje wiadomość z kluczem routingu (dla fan-out pattern)
    /// </summary>
    Task PublishAsync<T>(string topic, string routingKey, T message, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Publikuje wiele wiadomości w batch
    /// </summary>
    Task PublishBatchAsync<T>(string topic, IEnumerable<T> messages, CancellationToken cancellationToken = default);
}