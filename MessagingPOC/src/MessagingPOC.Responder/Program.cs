using MessagingPOC.Abstractions;
using MessagingPOC.RabbitMQ;
using MessagingPOC.Kafka;
using MessagingPOC.Responder.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// Konfiguracja - wybór brokera z appsettings
var brokerType = builder.Configuration["MessagingBroker"] ?? "RabbitMQ";

if (brokerType.Equals("RabbitMQ", StringComparison.OrdinalIgnoreCase))
{
    var rabbitConfig = builder.Configuration.GetSection("RabbitMQ").Get<RabbitMQConfiguration>()
                       ?? new RabbitMQConfiguration();

    builder.Services.AddSingleton(rabbitConfig);
    builder.Services.AddSingleton<IMessageProducer, RabbitMQProducer>();
    builder.Services.AddSingleton<IMessageConsumer, RabbitMQConsumer>();
}
else if (brokerType.Equals("Kafka", StringComparison.OrdinalIgnoreCase))
{
    var kafkaConfig = builder.Configuration.GetSection("Kafka").Get<KafkaConfiguration>()
                      ?? new KafkaConfiguration();

    builder.Services.AddSingleton(kafkaConfig);
    builder.Services.AddSingleton<IMessageProducer, KafkaProducer>();
    builder.Services.AddSingleton<IMessageConsumer, KafkaConsumer>();
}

// Rejestracja serwisu
if (brokerType.Equals("RabbitMQ", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<RequestReplyService>(sp =>
    {
        var consumer = sp.GetRequiredService<IMessageConsumer>();
        var producer = sp.GetRequiredService<IMessageProducer>();
        var logger = sp.GetRequiredService<ILogger<RequestReplyService>>();
        var rabbitConfig = sp.GetRequiredService<RabbitMQConfiguration>();
        return new RequestReplyService(consumer, producer, logger, rabbitConfig);
    });
}
else
{
    builder.Services.AddSingleton<RequestReplyService>();
}

var host = builder.Build();

// Uruchomienie serwisu Request-Reply
var requestReplyService = host.Services.GetRequiredService<RequestReplyService>();

// Uruchomienie w BackgroundService
_ = Task.Run(async () =>
{
    try
    {
        await requestReplyService.StartAsync(host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
});

Console.WriteLine($"Responder started with {brokerType} broker");
Console.WriteLine("Listening on request-topic...");

await host.RunAsync();