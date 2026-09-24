namespace AgentMesh.Models;

public sealed class AgentMeshConfiguration
{
    public string SandboxServiceUrl { get; init; } = string.Empty;

    public string SandboxName { get; init; } = string.Empty;

    public string AgentId { get; init; } = string.Empty;

    public IReadOnlyList<AgentConfigurationSummary> Agents { get; init; } = Array.Empty<AgentConfigurationSummary>();
}

public sealed class AgentConfigurationSummary
{
    public string AgentRole { get; init; } = string.Empty;

    public string Model { get; init; } = string.Empty;

    public string Provider { get; init; } = string.Empty;

    public decimal CostPerMillionInputTokens { get; init; }

    public decimal CostPerMillionOutputTokens { get; init; }

    public decimal? CostPerHour { get; init; }

    public double Temperature { get; init; }
}