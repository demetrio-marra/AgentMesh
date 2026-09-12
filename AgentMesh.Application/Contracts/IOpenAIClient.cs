using AgentMesh.Application.Contracts;
namespace AgentMesh.Application.Contracts
{
    public interface IOpenAIClient
    {
        Task<ChatClientResponse> GenerateResponseAsync(IEnumerable<string> userInput, CancellationToken cancellationToken = default);
        Task<ChatClientResponse> GenerateResponseAsync(IEnumerable<AgentMessage> messages, CancellationToken cancellationToken = default);
    }
}
