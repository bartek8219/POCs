using OpenAI.Chat;

namespace LlmAgentsSandbox.Services;

public class ToolRegistry
{
    private readonly Dictionary<string, Func<ChatToolCall, string>> _handlers = new(StringComparer.Ordinal);

    private readonly List<ChatTool> _tools = new();

    public IReadOnlyList<ChatTool> Tools => _tools;

    public void AddTool(ChatTool tool, Func<ChatToolCall, string> handler)
    {
        if (tool == null)
        {
            throw new ArgumentNullException(nameof(tool));
        }

        var name = tool.FunctionName;
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Tool function name is required.", nameof(tool));
        }

        if (_handlers.ContainsKey(name))
        {
            throw new InvalidOperationException($"Tool '{name}' is already registered.");
        }

        _tools.Add(tool);
        _handlers[name] = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    public ToolSession CreateSession(IEnumerable<(ChatTool Tool, Func<ChatToolCall, string> Handler)>? localTools = null)
    {
        var tools = new List<ChatTool>(_tools);
        var handlers = new Dictionary<string, Func<ChatToolCall, string>>(_handlers, StringComparer.Ordinal);

        if (localTools != null)
        {
            foreach (var (tool, handler) in localTools)
            {
                if (tool == null)
                {
                    throw new ArgumentNullException(nameof(localTools), "Tool cannot be null.");
                }

                var name = tool.FunctionName;
                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new ArgumentException("Tool function name is required.", nameof(localTools));
                }

                if (handlers.ContainsKey(name))
                {
                    throw new InvalidOperationException($"Tool '{name}' is already registered.");
                }

                tools.Add(tool);
                handlers[name] = handler ?? throw new ArgumentNullException(nameof(localTools), "Handler cannot be null.");
            }
        }

        return new ToolSession(tools, handlers);
    }
}

public class ToolSession
{
    private readonly IReadOnlyList<ChatTool> _tools;
    private readonly Dictionary<string, Func<ChatToolCall, string>> _handlers;

    public IReadOnlyList<ChatTool> Tools => _tools;

    public ToolSession(
        IReadOnlyList<ChatTool> tools,
        Dictionary<string, Func<ChatToolCall, string>> handlers)
    {
        _tools = tools;
        _handlers = handlers;
    }

    public string Resolve(ChatToolCall toolCall)
    {
        if (!_handlers.TryGetValue(toolCall.FunctionName, out var handler))
        {
            return "Tool not supported.";
        }

        return handler(toolCall);
    }
}

