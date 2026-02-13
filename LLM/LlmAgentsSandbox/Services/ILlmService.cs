using OpenAI.Chat;

namespace LlmAgentsSandbox.Services;

public interface ILlmService
{
    Task<string> CreateChatCompletionAsync(
        string systemPrompt,
        string userMessage,
        float? temperature = null,
        IReadOnlyList<ChatTool>? tools = null,
        ChatToolChoice? toolChoice = null,
        Func<ChatToolCall, string>? toolCallResolver = null,
        Action<string>? log = null,
        CancellationToken cancellationToken = default);
}

