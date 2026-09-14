using AgentMesh.Application.Contracts;
namespace AgentMesh.Application.Services.Agents
{
    public sealed class DocumentationAgent(
        IOpenAIClientFactory openAIClientFactory,
        Resilience resilience,
        ILogger<DocumentationAgent> logger,
        IAgentInputSerializer agentInputSerializer) : AbstractAgent<string>(logger,
            "Documentation",
            openAIClientFactory, 
            resilience,
            agentInputSerializer)
    {
        protected override IEnumerable<AgentInputParameterConfiguration> GetAgentInputParameterConfiguration()
        {
            return [
                new()
                {
                    ParameterType = typeof(RequestDateTimeParameter),
                    ParameterTags = [ParameterTags.AgentSystemParameterTag]
                },
                new() {
                    ParameterType = typeof(KnowledgeQueryResultParameter),
                    ParameterTags = [ParameterTags.AgentSystemParameterTag] 
                }
            ];
        }

        protected override string ParseStructuredResponse(string rawResponseText) => rawResponseText;
    }
}

