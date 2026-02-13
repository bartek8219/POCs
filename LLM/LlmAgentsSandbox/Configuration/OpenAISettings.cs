namespace LlmAgentsSandbox.Configuration;

public class OpenAISettings
{
    public const string SectionName = "OpenAI";
    
    public string DeploymentName { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public float DefaultTemperature { get; set; } = 0.7f;
}
