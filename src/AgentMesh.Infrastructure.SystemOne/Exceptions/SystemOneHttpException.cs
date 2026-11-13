using System.Net;

namespace AgentMesh.Infrastructure.SystemOne;

public sealed class SystemOneProviderError(IReadOnlyList<string> details)
{
    public IReadOnlyList<string> Details { get; } = details;
}

public sealed class SystemOneHttpException : HttpRequestException
{
    public SystemOneHttpException(HttpStatusCode statusCode, SystemOneProviderError? providerError)
        : base($"SystemOne provider returned HTTP {(int)statusCode}.", null, statusCode)
    {
        ProviderError = providerError;
    }

    public SystemOneProviderError? ProviderError { get; }
}
