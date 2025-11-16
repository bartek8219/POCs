namespace MessagingPOC.Responder.Models;

public record RequestMessage
{
    public string Question { get; init; } = string.Empty;
}