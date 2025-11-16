using System.Text;
using System.Text.Json;
using MessagingPOC.Abstractions;
using MessagingPOC.RabbitMQ;
using MessagingPOC.Responder.Models;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace MessagingPOC.Responder.Services;

public class RequestReplyService : IDisposable, IAsyncDisposable
{
    private readonly IMessageConsumer _consumer;
    private readonly IMessageProducer _producer;
    private readonly ILogger<RequestReplyService> _logger;
    private readonly RabbitMQConfiguration? _rabbitConfig;
    private IConnection? _rabbitConnection;
    private IChannel? _rabbitChannel;

    public RequestReplyService(
        IMessageConsumer consumer,
        IMessageProducer producer,
        ILogger<RequestReplyService> logger,
        RabbitMQConfiguration? rabbitConfig = null)
    {
        _consumer = consumer;
        _producer = producer;
        _logger = logger;
        _rabbitConfig = rabbitConfig;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("RequestReplyService starting...");

        // Jeśli używamy RabbitMQ, używamy bezpośrednio RabbitMQ API aby mieć dostęp do nagłówków
        if (_rabbitConfig != null)
        {
            await StartRabbitMQRequestReplyAsync(cancellationToken);
        }
        else
        {
            // Dla innych brokerów używamy abstrakcji (np. Kafka)
            await StartGenericRequestReplyAsync(cancellationToken);
        }
    }

    private async Task StartRabbitMQRequestReplyAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _rabbitConfig!.HostName,
            Port = _rabbitConfig.Port,
            UserName = _rabbitConfig.UserName,
            Password = _rabbitConfig.Password,
            VirtualHost = _rabbitConfig.VirtualHost
        };

        _rabbitConnection = await factory.CreateConnectionAsync();
        _rabbitChannel = await _rabbitConnection.CreateChannelAsync();

        // Deklaracja kolejki request-topic
        await _rabbitChannel.QueueDeclareAsync(
            queue: "request-topic",
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        // Ustawienie prefetch
        await _rabbitChannel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken);

        _logger.LogInformation("Listening for requests on request-topic (RabbitMQ)...");

        // Konsumpcja wiadomości z dostępem do nagłówków
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var basicGetResult = await _rabbitChannel.BasicGetAsync("request-topic", autoAck: false, cancellationToken);

                if (basicGetResult != null)
                {
                    try
                    {
                        var body = basicGetResult.Body.ToArray();
                        var json = Encoding.UTF8.GetString(body);
                        var envelope = JsonSerializer.Deserialize<MessageEnvelope<RequestMessage>>(json);

                        if (envelope != null && envelope.Payload != null)
                        {
                            _logger.LogInformation($"Received request: {envelope.Payload.Question}");

                            // Odczytujemy ReplyTo i CorrelationId z BasicProperties
                            var replyTo = basicGetResult.BasicProperties?.ReplyTo;
                            var correlationId = basicGetResult.BasicProperties?.CorrelationId ?? envelope.CorrelationId;

                            if (string.IsNullOrEmpty(replyTo))
                            {
                                _logger.LogWarning("ReplyTo header is missing, cannot send response");
                                await _rabbitChannel.BasicAckAsync(basicGetResult.DeliveryTag, multiple: false, cancellationToken);
                                continue;
                            }

                            if (string.IsNullOrEmpty(correlationId))
                            {
                                _logger.LogWarning("CorrelationId header is missing");
                                correlationId = envelope.CorrelationId;
                            }

                            // Przetworzenie requestu
                            var response = ProcessRequest(envelope.Payload);

                            _logger.LogInformation($"Sending response: {response.Result} to queue: {replyTo} with CorrelationId: {correlationId}");

                            // Wysyłamy odpowiedź na kolejkę z ReplyTo z nagłówka
                            var responseEnvelope = new MessageEnvelope<ResponseMessage>
                            {
                                CorrelationId = correlationId,
                                Payload = response
                            };

                            var responseBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(responseEnvelope));

                            // Ustawiamy CorrelationId w BasicProperties dla odpowiedzi
                            var responseProperties = new BasicProperties
                            {
                                CorrelationId = correlationId,
                                Persistent = false
                            };

                            // Wysyłamy odpowiedź bezpośrednio na kolejkę (nie przez exchange)
                            await _rabbitChannel.BasicPublishAsync(
                                exchange: string.Empty,
                                routingKey: replyTo,
                                mandatory: false,
                                basicProperties: responseProperties,
                                body: new ReadOnlyMemory<byte>(responseBody),
                                cancellationToken: cancellationToken);

                            await _rabbitChannel.BasicAckAsync(basicGetResult.DeliveryTag, multiple: false, cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing request");
                        await _rabbitChannel.BasicNackAsync(basicGetResult.DeliveryTag, multiple: false, requeue: true, cancellationToken);
                    }
                }
                else
                {
                    // Czekaj jeśli brak wiadomości
                    await Task.Delay(100, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in request-reply loop");
                await Task.Delay(1000, cancellationToken);
            }
        }
    }

    private async Task StartGenericRequestReplyAsync(CancellationToken cancellationToken)
    {
        // Dla innych brokerów (np. Kafka) używamy abstrakcji
        // Subskrybujemy temat request-topic i nasłuchujemy wiadomości zawierających RequestMessage
        await _consumer.StartConsumingAsync<RequestMessage>(
            "request-topic",
            "",
            async requestMessage =>
            {
                try
                {
                    _logger.LogInformation($"Received request: {requestMessage.Question}");

                    // Przetworzenie requestu
                    var response = ProcessRequest(requestMessage);

                    _logger.LogInformation($"Sending response: {response.Result}");

                    // Dla innych brokerów używamy standardowego publish
                    await _producer.PublishAsync("reply-topic", response);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing request");
                }
            },
            cancellationToken);

        // Czekamy na anulowanie
        await Task.Delay(Timeout.Infinite, cancellationToken);
    }

    private ResponseMessage ProcessRequest(RequestMessage request)
    {
        // Tutaj można umieścić logikę przetwarzania businessowego
        // Dla celów POC zwracamy prostą odpowiedź

        var result = request.Question switch
        {
            "ping" => "pong",
            "hello" => "Hello back!",
            "time" => DateTime.UtcNow.ToString("O"),
            _ => $"Processed: {request.Question}"
        };

        return new ResponseMessage { Result = result };
    }

    public async ValueTask DisposeAsync()
    {
        if (_rabbitChannel != null)
        {
            await _rabbitChannel.CloseAsync();
            await _rabbitChannel.DisposeAsync();
        }

        if (_rabbitConnection != null)
        {
            await _rabbitConnection.CloseAsync();
            await _rabbitConnection.DisposeAsync();
        }
    }

    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }
}
