using Microsoft.AspNetCore.Mvc;
using MessagingPOC.Abstractions;
using Microsoft.Extensions.Logging;

namespace MessagingPOC.Producer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessageController : ControllerBase
{
    private readonly IMessageProducer _producer;
    private readonly IRequestReplyClient _requestReplyClient;
    private readonly ILogger<MessageController> _logger;

    public MessageController(
        IMessageProducer producer,
        IRequestReplyClient requestReplyClient,
        ILogger<MessageController> logger)
    {
        _producer = producer;
        _requestReplyClient = requestReplyClient;
        _logger = logger;
    }

    /// <summary>
    /// Wysyła prostą wiadomość (fan-out pattern)
    /// </summary>
    [HttpPost("publish")]
    public async Task<IActionResult> PublishMessage([FromBody] SampleMessage message)
    {
        try
        {
            await _producer.PublishAsync("fanout-topic", message);
            _logger.LogInformation($"Published message: {message.Content}");
            return Ok(new { success = true, message = "Message published" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing message");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Wysyła wiele wiadomości w batch
    /// </summary>
    [HttpPost("publish-batch")]
    public async Task<IActionResult> PublishBatch([FromBody] BatchRequest request)
    {
        try
        {
            var messages = Enumerable.Range(1, request.Count)
                .Select(i => new SampleMessage 
                { 
                    Content = $"{request.MessagePrefix} #{i}",
                    Timestamp = DateTime.UtcNow 
                });

            await _producer.PublishBatchAsync("fanout-topic", messages);
            _logger.LogInformation($"Published {request.Count} messages");
            
            return Ok(new { success = true, count = request.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing batch");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Request-Reply pattern
    /// </summary>
    [HttpPost("request-reply")]
    public async Task<IActionResult> SendRequest([FromBody] RequestMessage request)
    {
        try
        {
            var response = await _requestReplyClient.SendRequestAsync<RequestMessage, ResponseMessage>(
                "request-topic",
                request,
                TimeSpan.FromSeconds(30));

            _logger.LogInformation($"Received response: {response.Result}");
            
            return Ok(new { success = true, response });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in request-reply");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}

public record SampleMessage
{
    public string Content { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}

public record BatchRequest
{
    public int Count { get; init; }
    public string MessagePrefix { get; init; } = "Message";
}

public record RequestMessage
{
    public string Question { get; init; } = string.Empty;
}

public record ResponseMessage
{
    public string Result { get; init; } = string.Empty;
}
