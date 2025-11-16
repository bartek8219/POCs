using MessagingPOC.Abstractions;
using MessagingPOC.RabbitMQ;
using MessagingPOC.Kafka;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Konfiguracja - wybór brokera z appsettings
var brokerType = builder.Configuration["MessagingBroker"] ?? "RabbitMQ";

if (brokerType.Equals("RabbitMQ", StringComparison.OrdinalIgnoreCase))
{
    var rabbitConfig = builder.Configuration.GetSection("RabbitMQ").Get<RabbitMQConfiguration>() 
                       ?? new RabbitMQConfiguration();
    
    builder.Services.AddSingleton(rabbitConfig);
    builder.Services.AddSingleton<IMessageProducer>(sp => 
    {
        var config = sp.GetRequiredService<RabbitMQConfiguration>();
        return new RabbitMQProducer(config);
    });
    builder.Services.AddSingleton<IRequestReplyClient, RabbitMQRequestReplyClient>();
}
else if (brokerType.Equals("Kafka", StringComparison.OrdinalIgnoreCase))
{
    var kafkaConfig = builder.Configuration.GetSection("Kafka").Get<KafkaConfiguration>() 
                      ?? new KafkaConfiguration();
    
    builder.Services.AddSingleton(kafkaConfig);
    builder.Services.AddSingleton<IMessageProducer, KafkaProducer>();
    builder.Services.AddSingleton<IRequestReplyClient, KafkaRequestReplyClient>();
}

// Konfiguracja Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/responder-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Services.AddLogging(config => config.AddSerilog());

var app = builder.Build();

// Konfiguracja portu
//app.Urls.Add("http://localhost:5000");

// Włącz Swagger zawsze (nie tylko w Development)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MessagingPOC API v1");
    c.RoutePrefix = "swagger";
});

app.UseRouting();
app.UseAuthorization();
app.MapControllers();


Console.WriteLine($"Producer started with {brokerType} broker");
Console.WriteLine($"Listening on: http://localhost:5000");
Console.WriteLine($"Swagger UI: http://localhost:5000/swagger");
Console.WriteLine($"API Info: http://localhost:5000/api");
app.Run();