using LlmAgentsSandbox.Agents.Core;
using LlmAgentsSandbox.Services;
using LlmAgentsSandbox.Tools;
using OpenAI.Chat;

namespace LlmAgentsSandbox.Agents;

public class TruthTellerAgent : BaseAgent
{
    private readonly ToolRegistry _toolRegistry;

    public override string Name => "Agent prawdomowca";
    public override string Description => "Agent ktory zawsze odpowiada zgodnie z prawda";

    // Custom system prompt dla tego agenta
    public override string SystemPrompt =>
        "Zawsze zwracaj wynik. " +
        //"Masz dostep do narzedzia calculate do prostych obliczen (add, subtract). " +
        //"Jeśli masz dostęp do narzędzia calculate to zawsze zwracasz wynik zgodny z tym wyliczeniem, nawet jeśli wg. twojej wiedzy jest to nieprawda." +
        //"Zawsze skomentuj otrzymany wynik ale nie wprost. " +
        "";

    public string UserPrompt => "Mam 10 beczek rumu. Jeśli sprzedam 3 to ile mi zostanie?";

    /// <summary>
    /// Konstruktor TruthTellerAgent
    /// </summary>
    /// <param name="llmService"></param>
    /// <param name="toolRegistry"></param>
    public TruthTellerAgent(ILlmService llmService, ToolRegistry toolRegistry) : base(llmService)
    {
        _toolRegistry = toolRegistry;
    }

    public override async Task<string> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var toolSession = _toolRegistry.CreateSession([
            (CalculatorToolDefinition.Tool, CalculatorToolDefinition.Handle)
        ]);

        var result = await LlmService.CreateChatCompletionAsync(
            SystemPrompt,
            UserPrompt,
            temperature: 0.7f,
            tools: toolSession.Tools,
            toolChoice: ChatToolChoice.CreateAutoChoice(),
            toolCallResolver: toolSession.Resolve,
            log: Console.WriteLine,
            cancellationToken);

        return result;
    }
}
