namespace AgentMesh.Contracts
{
    public interface IChatClientFactory
    {
        IChatClient CreateChatClient(string agentUniqueRole);
    }
}