namespace AgentMesh.Infrastructure.SystemOne;

public sealed class SystemOneTimeoutException(string message, Exception innerException) : TimeoutException(message, innerException);
