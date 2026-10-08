using AgentMesh.Application.Contracts;
namespace AgentMesh.Infrastructure.ChatCompletions
{
    public class ChatCompletionsClientFactory(IEnumerable<AgentFlatConfigurationRecord> agentFlatConfigurationRecords) : 
        IOpenAIClientFactory
    {
        public IChatCompletionsClient CreateOpenAIClient(string agentUniqueRole)
        {
            var cfg = GetAgentConfiguration(agentUniqueRole);
            return new ChatCompletionsClient(cfg.ProviderModelName, 
                cfg.ProviderApiKey,
                cfg.ProviderEndpoint, 
                cfg.Temperature,
                cfg.SystemPrompt);
        }

        private AgentFlatConfigurationRecord GetAgentConfiguration(string agentUniqueRole)
        {
            var cfg = agentFlatConfigurationRecords.Single(x => string.Compare(agentUniqueRole, x.AgentUniqueRole, true) == 0);
            return cfg;
        }
    }
}
