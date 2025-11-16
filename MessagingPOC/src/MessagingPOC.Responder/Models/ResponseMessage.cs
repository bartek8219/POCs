namespace MessagingPOC.Responder.Models;

public record ResponseMessage
{
    public string Result { get; init; } = string.Empty;
}