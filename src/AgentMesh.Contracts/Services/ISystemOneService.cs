using AgentMesh.Contracts.Models.SystemOne;

namespace AgentMesh.Contracts.Services;

public interface ISystemOneService
{
    Task<SystemOneResult> EvaluateAsync(SystemOneInput input, CancellationToken cancellationToken = default);
}
