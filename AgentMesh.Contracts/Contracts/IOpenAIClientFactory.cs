namespace AgentMesh.Application.Contracts
{
    public interface IOpenAIClientFactory
    {
        IChatCompletionsClient CreateOpenAIClient(string agentUniqueRole);
    }
}