namespace AgentMesh.Contracts.Models.SystemOne;

public sealed record SystemOneResult(
    string Model,
    IReadOnlyDictionary<string, SystemOneAnswer> Answers,
    SystemOneUsage Usage);

public sealed record SystemOneUsage(int InputTokens, int OutputTokens);

public abstract record SystemOneAnswer;

public sealed record SystemOneNoulAnswer(double YesProbability) : SystemOneAnswer;

public sealed record SystemOneChoiceAnswer(
    string Choice,
    double Confidence,
    IReadOnlyDictionary<string, double> Probabilities) : SystemOneAnswer;

public sealed record SystemOneScoreAnswer(
    double Score,
    double Confidence,
    IReadOnlyDictionary<string, SystemOneJsonValue> Legend,
    IReadOnlyDictionary<string, double> Probabilities) : SystemOneAnswer;
