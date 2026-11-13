namespace AgentMesh.Infrastructure.SystemOne;

public sealed class SystemOneValidationException(string message) : ArgumentException(message, "input");
