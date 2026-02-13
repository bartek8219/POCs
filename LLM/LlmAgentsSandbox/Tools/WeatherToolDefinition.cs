using System.Text.Json;
using OpenAI.Chat;

namespace LlmAgentsSandbox.Tools;

public static class WeatherToolDefinition
{
    public const string Name = "get_weather";

    public static readonly ChatTool Tool = ChatTool.CreateFunctionTool(
        functionName: Name,
        functionDescription: "Zwraca opis pogody.",
        functionParameters: BinaryData.FromString(
            "{" +
            "\"type\":\"object\"," +
            "\"properties\":{" +
            "\"location\":{\"type\":\"string\"}," +
            "\"date\":{\"type\":\"string\"}" +
            "}," +
            "\"additionalProperties\":false," +
            "\"required\":[\"location\"]" +
            "}"),
        functionSchemaIsStrict: false);

    public static string Handle(ChatToolCall toolCall)
    {
        using var document = JsonDocument.Parse(toolCall.FunctionArguments);
        var root = document.RootElement;

        var location = root.GetProperty("location").GetString() ?? "Unknown";
        var date = root.TryGetProperty("date", out var dateElement)
            ? dateElement.GetString()
            : null;

        var when = string.IsNullOrWhiteSpace(date) ? "dzisiaj" : date;
        return $"Pogoda w {location} na {when}: slonecznie, 22C, bez opadow.";
    }
}

