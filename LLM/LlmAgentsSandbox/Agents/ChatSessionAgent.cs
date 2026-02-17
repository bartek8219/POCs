using LlmAgentsSandbox.Agents.Core;
using LlmAgentsSandbox.Services;
using LlmAgentsSandbox.Tools;
using OpenAI.Chat;

namespace LlmAgentsSandbox.Agents;

public class ChatSessionAgent : BaseAgent
{
    private const ConsoleColor UserMessageColor = ConsoleColor.Cyan;

    private readonly ToolRegistry _toolRegistry;
    private readonly List<ChatMessage> _history;

    public override string Name => "Agent konwersacyjny";
    public override string Description => "Agent prowadzacy wieloturnowa rozmowe z pamiecia historii";

    public override string SystemPrompt =>
        "Jestes pomocnym asystentem AI. " +
        "Pamietaj kontekst rozmowy i odpowiadaj po polsku.";

    public ChatSessionAgent(ILlmService llmService, ToolRegistry toolRegistry) : base(llmService)
    {
        _toolRegistry = toolRegistry;
        _history = [new SystemChatMessage(SystemPrompt)];
    }

    public override async Task<string> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("--- Chat session agent ---");
        Console.WriteLine("Wpisz wiadomosc i nacisnij Enter. Zakoncz: Ctrl+C.\n");

        var toolSession = _toolRegistry.CreateSession(
        [
            (CalculatorToolDefinition.Tool, CalculatorToolDefinition.Handle)
        ]);

        while (true)
        {
            Console.ForegroundColor = UserMessageColor;
            Console.Write("[YOU]: ");
            Console.ResetColor();
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            _history.Add(new UserChatMessage(input));

            var result = await LlmService.CreateChatCompletionAsync(
                _history,
                tools: toolSession.Tools,
                toolChoice: ChatToolChoice.CreateAutoChoice(),
                toolCallResolver: toolSession.Resolve,
                cancellationToken: cancellationToken);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[ASSISTANT]: {result}\n");
            Console.ResetColor();
        }
    }
}
