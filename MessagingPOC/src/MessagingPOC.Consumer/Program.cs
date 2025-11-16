using MessagingPOC.Abstractions;
using MessagingPOC.RabbitMQ;
using MessagingPOC.Kafka;
using MessagingPOC.Consumer.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Konfiguracja - wybór brokera
var brokerType = builder.Configuration["MessagingBroker"] ?? "RabbitMQ";

if (brokerType.Equals("RabbitMQ", StringComparison.OrdinalIgnoreCase))
{
    var rabbitConfig = builder.Configuration.GetSection("RabbitMQ").Get<RabbitMQConfiguration>() 
                       ?? new RabbitMQConfiguration();
    
    builder.Services.AddSingleton(rabbitConfig);
    builder.Services.AddSingleton<IMessageConsumer, RabbitMQConsumer>();
}
else if (brokerType.Equals("Kafka", StringComparison.OrdinalIgnoreCase))
{
    var kafkaConfig = builder.Configuration.GetSection("Kafka").Get<KafkaConfiguration>() 
                      ?? new KafkaConfiguration();
    
    builder.Services.AddSingleton(kafkaConfig);
    builder.Services.AddSingleton<IMessageConsumer, KafkaConsumer>();
}

builder.Services.AddHostedService<MessageWorker>();

var host = builder.Build();
Console.WriteLine($"Consumer started with {brokerType} broker");
host.Run();