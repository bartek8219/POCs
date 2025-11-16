namespace MessagingPOC.Abstractions;

public class MessageEnvelope<T>
{
    public string MessageId { get; set; } = Guid.NewGuid().ToString();
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string MessageType { get; set; } = typeof(T).Name;
    public T Payload { get; set; } = default!;
    public Dictionary<string, string> Headers { get; set; } = new();
}