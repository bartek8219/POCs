using LlmAgentsSandbox.Services;

namespace LlmAgentsSandbox.Agents.Core;

public abstract class BaseAgent : IAgent
{
    protected readonly ILlmService LlmService;

    protected BaseAgent(ILlmService llmService)
    {
        LlmService = llmService;
    }

    public abstract string Name { get; }
    public abstract string Description { get; }

    public virtual string SystemPrompt =>
        "Jestes pomocnym asystentem AI. " +
        "Jesli brakuje danych do narzedzia, zadaj pytanie doprecyzowujace zamiast zgadywac.";

    public abstract Task<string> ExecuteAsync(CancellationToken cancellationToken = default);
}

