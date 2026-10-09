namespace AgentMesh.Application.Services.Helpers
{
    public interface IAgentInputSerializer
    {
        IEnumerable<AgentMessage> SerializeInput(IReadOnlyDictionary<Type, object?> parameters, 
            IEnumerable<AgentInputParameterConfiguration> parameterTagsConfiguration);
    }
}
