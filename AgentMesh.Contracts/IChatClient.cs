using AgentMesh.Contracts.Models.ChatClient;
using AgentMesh.Contracts.Models.ChatMessages;

namespace AgentMesh.Contracts
{
    public interface IChatClient
    {
        Task<ChatClientResponse> GenerateResponseAsync(IEnumerable<string> userInput, CancellationToken cancellationToken = default);
        Task<ChatClientResponse> GenerateResponseAsync(IEnumerable<AgentMessage> messages, CancellationToken cancellationToken = default);
    }
}