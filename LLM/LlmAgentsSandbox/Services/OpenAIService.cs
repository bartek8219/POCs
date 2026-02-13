using System.ClientModel;
using System.Reflection;
using LlmAgentsSandbox.Configuration;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;

namespace LlmAgentsSandbox.Services;

public class OpenAIService : ILlmService
{
    private readonly ChatClient _client;
    private readonly OpenAISettings _settings;

    public OpenAIService(IOptions<OpenAISettings> settings)
    {
        _settings = settings.Value;
        
        _client = new ChatClient(
            model: _settings.DeploymentName,
            credential: new ApiKeyCredential(_settings.ApiKey),
            options: new OpenAIClientOptions()
            {
                Endpoint = new Uri(_settings.Endpoint),
            });
    }

    public async Task<string> CreateChatCompletionAsync(
        string systemPrompt,
        string userMessage,
        float? temperature = null,
        IReadOnlyList<ChatTool>? tools = null,
        ChatToolChoice? toolChoice = null,
        Func<ChatToolCall, string>? toolCallResolver = null,
        Action<string>? log = null,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userMessage)
        };

        var options = new ChatCompletionOptions
        {
            Temperature = temperature ?? _settings.DefaultTemperature
        };

        if (tools != null)
        {
            foreach (var tool in tools)
            {
                options.Tools.Add(tool);
            }
        }

        if (toolChoice != null)
        {
            options.ToolChoice = toolChoice;
        }

        LogIfEnabled(log, "LLM request:");
        LogIfEnabled(log, $"- System: {systemPrompt}");
        LogIfEnabled(log, $"- User: {userMessage}");
        LogIfEnabled(log, $"- Tools: {(tools == null ? 0 : tools.Count)}");
        LogIfEnabled(log, $"- Tool choice: {(toolChoice == null ? "null" : toolChoice.ToString())}");

        var completion = await _client.CompleteChatAsync(messages, options, cancellationToken);

        string? lastToolOutput = null;
        if (completion.Value.ToolCalls != null && completion.Value.ToolCalls.Count > 0)
        {
            if (toolCallResolver == null)
            {
                throw new InvalidOperationException("Model requested a tool call, but no tool resolver was provided.");
            }

            LogIfEnabled(log, $"LLM tool calls: {completion.Value.ToolCalls.Count}");
            messages.Add(ChatMessage.CreateAssistantMessage(completion.Value.ToolCalls));

            foreach (var toolCall in completion.Value.ToolCalls)
            {
                LogIfEnabled(log, $"- Tool call: {toolCall.FunctionName} args={toolCall.FunctionArguments}");
                var output = toolCallResolver(toolCall);
                lastToolOutput = output;
                LogIfEnabled(log, $"- Tool output: {output}");
                messages.Add(new ToolChatMessage(GetToolCallId(toolCall), output));
            }

            completion = await _client.CompleteChatAsync(messages, options, cancellationToken);
        }

        var text = GetFirstTextOrEmpty(completion);
        if (!string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        if (!string.IsNullOrWhiteSpace(lastToolOutput))
        {
            LogIfEnabled(log, "LLM returned empty text after tool calls. Returning empty string.");
            return string.Empty;
        }

        if (completion.Value.ToolCalls != null && completion.Value.ToolCalls.Count > 0)
        {
            LogIfEnabled(log, "LLM returned tool calls but no final text. Returning empty string.");
        }
        else
        {
            LogIfEnabled(log, "LLM returned empty text.");
        }

        return string.Empty;
    }

    private static string GetToolCallId(ChatToolCall toolCall)
    {
        var type = toolCall.GetType();
        var idProperty = type.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance)
            ?? type.GetProperty("ToolCallId", BindingFlags.Public | BindingFlags.Instance);

        var id = idProperty?.GetValue(toolCall) as string;
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new InvalidOperationException("Tool call id not found.");
        }

        return id;
    }

    private static string GetFirstTextOrEmpty(ChatCompletion completion)
    {
        if (completion.Content == null || completion.Content.Count == 0)
        {
            return string.Empty;
        }

        return completion.Content[0].Text ?? string.Empty;
    }

    private static void LogIfEnabled(Action<string>? log, string message)
    {
        log?.Invoke(message);
    }
}

