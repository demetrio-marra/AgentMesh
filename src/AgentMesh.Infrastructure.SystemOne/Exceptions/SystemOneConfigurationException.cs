namespace AgentMesh.Infrastructure.SystemOne;

public sealed class SystemOneConfigurationException(string message) : InvalidOperationException(message);
