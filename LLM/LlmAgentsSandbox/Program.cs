using LlmAgentsSandbox.Agents;
using LlmAgentsSandbox.Configuration;
using LlmAgentsSandbox.Services;
using LlmAgentsSandbox.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LlmAgentsSandbox;

public class Program
{
    private const ConsoleColor UserMessageColor = ConsoleColor.Cyan;

    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // Load local overrides regardless of DOTNET_ENVIRONMENT value.
        builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);
        builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

        ConfigureServices(builder.Services, builder.Configuration);
        using var host = builder.Build();
        var serviceProvider = host.Services;

        // Run example agents
        Console.WriteLine("=== LLM Agents POC ===\n");

        try
        {
            if (args.Contains("--chat", StringComparer.OrdinalIgnoreCase))
            {
                await RunChatSessionAgentExample(serviceProvider);
            }
            else
            {
                await RunTruthTellerAgentExample(serviceProvider);
            }
            
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
        services.AddTransient<ChatSessionAgent>();
        
        // Dodaj tutaj kolejne agenty:
        // services.AddTransient<TranslationAgent>();
        // services.AddTransient<SummaryAgent>();
    }

    private static async Task RunTruthTellerAgentExample(IServiceProvider serviceProvider)
    {
        Console.WriteLine("--- Truth teller agent example ---");
        
        var truthTellerAgent = serviceProvider.GetRequiredService<TruthTellerAgent>();
        Console.WriteLine($"Running: {truthTellerAgent.Name}");
        Console.WriteLine($"Description: {truthTellerAgent.Description}\n");
        Console.ForegroundColor = UserMessageColor;
        Console.WriteLine($"[YOU]: {truthTellerAgent.UserPrompt}\n");
        Console.ResetColor();

        var result = await truthTellerAgent.ExecuteAsync();
        
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[ASSISTANT]: {result}");
        Console.ResetColor();
        Console.WriteLine();
    }

    private static async Task RunChatSessionAgentExample(IServiceProvider serviceProvider)
    {
        Console.WriteLine("--- Chat session agent example ---");

        var chatSessionAgent = serviceProvider.GetRequiredService<ChatSessionAgent>();
        Console.WriteLine($"Running: {chatSessionAgent.Name}");
        Console.WriteLine($"Description: {chatSessionAgent.Description}\n");

        await chatSessionAgent.ExecuteAsync();
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

