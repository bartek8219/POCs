namespace LlmAgentsSandbox.Agents.Core;

public interface IAgent
{
    string Name { get; }
    string Description { get; }
    Task<string> ExecuteAsync(CancellationToken cancellationToken = default);
}

