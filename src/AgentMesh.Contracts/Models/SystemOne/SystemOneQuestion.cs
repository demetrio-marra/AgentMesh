namespace AgentMesh.Contracts.Models.SystemOne;

public abstract record SystemOneQuestion;

public sealed record SystemOneNoulQuestion : SystemOneQuestion
{
    public SystemOneJsonValue? Instructions { get; init; }

    public IReadOnlyDictionary<bool, SystemOneJsonValue> Criteria { get; init; } =
        new Dictionary<bool, SystemOneJsonValue>();
}

public sealed record SystemOneChoiceQuestion : SystemOneQuestion
{
    public SystemOneJsonValue? Instructions { get; init; }

    public IReadOnlyDictionary<string, SystemOneJsonValue> Criteria { get; init; } =
        new Dictionary<string, SystemOneJsonValue>(StringComparer.Ordinal);
}

public sealed record SystemOneScoreQuestion : SystemOneQuestion
{
    public SystemOneJsonValue? Instructions { get; init; }

    public IReadOnlyList<SystemOneJsonValue> Criteria { get; init; } = [];
}
