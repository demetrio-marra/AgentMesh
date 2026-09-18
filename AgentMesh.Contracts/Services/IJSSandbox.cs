using AgentMesh.Models.CodeSandbox;

namespace AgentMesh.Services
{
    public interface IJSSandbox
    {
        Task<CodeSandboxOutput> RunCode(string agentId, string code);
    }
}
