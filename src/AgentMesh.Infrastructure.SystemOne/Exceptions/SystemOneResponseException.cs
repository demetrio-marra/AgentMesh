namespace AgentMesh.Infrastructure.SystemOne;

public sealed class SystemOneResponseException : Exception
{
    public SystemOneResponseException(string message) : base(message)
    {
    }

    public SystemOneResponseException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
