using LlmAgentsSandbox.Agents;
using LlmAgentsSandbox.Configuration;
using LlmAgentsSandbox.Services;
using LlmAgentsSandbox.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LlmAgentsSandbox;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Build configuration
        var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Production";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        // Setup DI container
        var services = new ServiceCollection();
        ConfigureServices(services, configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Run example agents
        Console.WriteLine("=== LLM Agents POC ===\n");

        try
        {
            await RunTruthTellerAgentExample(serviceProvider);
            
            // Dodaj tutaj kolejne przyklady agent�w:
            // await RunTranslationAgentExample(serviceProvider);
            // await RunSummaryAgentExample(serviceProvider);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[ERROR]: {ex.Message}");
            Console.ResetColor();
        }

        Console.WriteLine("\n=== End of POC ===");
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Configuration
        services.Configure<OpenAISettings>(configuration.GetSection(OpenAISettings.SectionName));

        // Services
        services.AddSingleton<ILlmService, OpenAIService>();
        services.AddSingleton<ToolRegistry>(sp =>
        {
            var registry = new ToolRegistry();
            registry.AddTool(WeatherToolDefinition.Tool, WeatherToolDefinition.Handle);
            return registry;
        });

        // Agents
        services.AddTransient<TruthTellerAgent>();
        
        // Dodaj tutaj kolejne agenty:
        // services.AddTransient<TranslationAgent>();
        // services.AddTransient<SummaryAgent>();
    }

    private static async Task RunTruthTellerAgentExample(ServiceProvider serviceProvider)
    {
        Console.WriteLine("--- Truth teller agent example ---");
        
        var truthTellerAgent = serviceProvider.GetRequiredService<TruthTellerAgent>();
        Console.WriteLine($"Running: {truthTellerAgent.Name}");
        Console.WriteLine($"Description: {truthTellerAgent.Description}\n");

        var result = await truthTellerAgent.ExecuteAsync();
        
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[ASSISTANT]: {result}");
        Console.ResetColor();
        Console.WriteLine();
    }
    
    // Przykladowe metody dla kolejnych agent�w:
    /*
    private static async Task RunTranslationAgentExample(ServiceProvider serviceProvider)
    {
        Console.WriteLine("--- Translation Agent Example ---");
        var agent = serviceProvider.GetRequiredService<TranslationAgent>();
        var result = await agent.ExecuteAsync();
        Console.WriteLine($"[ASSISTANT]: {result}\n");
    }
    */
}

