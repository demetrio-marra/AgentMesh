namespace AgentMesh.Application.Services
{
    public interface IEWAgenticStep : IEWStep
    {
        string? AgentName { get; }

        bool CountInputTokensAsContextTokens { get; }

        bool CountOutputTokensAsContextTokens { get; }
    }
}
