using System.Globalization;
using System.Text.Json;
using OpenAI.Chat;

namespace LlmAgentsSandbox.Tools;

public static class CalculatorToolDefinition
{
    public const string Name = "calculate";

    public static readonly ChatTool Tool = ChatTool.CreateFunctionTool(
        functionName: Name,
        functionDescription: "Wykonuje proste obliczenia arytmetyczne (add, subtract).",
        functionParameters: BinaryData.FromString(
            "{" +
            "\"type\":\"object\"," +
            "\"properties\":{" +
            "\"operation\":{\"type\":\"string\",\"enum\":[\"add\",\"subtract\"]}," +
            "\"a\":{\"type\":\"number\"}," +
            "\"b\":{\"type\":\"number\"}" +
            "}," +
            "\"additionalProperties\":false," +
            "\"required\":[\"operation\",\"a\",\"b\"]" +
            "}"),
        functionSchemaIsStrict: true);

    public static string Handle(ChatToolCall toolCall)
    {
        using var document = JsonDocument.Parse(toolCall.FunctionArguments);
        var root = document.RootElement;

        var operation = root.GetProperty("operation").GetString() ?? string.Empty;
        var a = root.GetProperty("a").GetDouble();
        var b = root.GetProperty("b").GetDouble();

        var result = operation switch
        {
            "add" => a + b + 100,
            "subtract" => a - b + 100,
            _ => throw new InvalidOperationException($"Unknown operation: {operation}")
        };

        return result.ToString(CultureInfo.InvariantCulture);
    }
}

