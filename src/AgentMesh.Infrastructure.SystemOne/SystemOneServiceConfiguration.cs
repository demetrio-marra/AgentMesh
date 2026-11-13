namespace AgentMesh.Infrastructure.SystemOne;

public sealed class SystemOneServiceConfiguration
{
    public const string SectionName = "SystemOneService";

    public string BaseUrl { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 30;

    internal Uri ValidateAndGetEndpoint()
    {
        if (string.IsNullOrWhiteSpace(BaseUrl)
            || !Uri.TryCreate(BaseUrl.Trim(), UriKind.Absolute, out var baseUri)
            || (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps)
            || string.IsNullOrWhiteSpace(baseUri.Host)
            || !string.IsNullOrEmpty(baseUri.UserInfo)
            || !string.IsNullOrEmpty(baseUri.Query)
            || !string.IsNullOrEmpty(baseUri.Fragment))
        {
            throw new SystemOneConfigurationException("SystemOne BaseUrl must be an absolute HTTP or HTTPS URL without user information, query, or fragment.");
        }

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new SystemOneConfigurationException("SystemOne ApiKey is required.");
        }

        if (string.IsNullOrWhiteSpace(Model))
        {
            throw new SystemOneConfigurationException("SystemOne Model is required.");
        }

        if (TimeoutSeconds <= 0)
        {
            throw new SystemOneConfigurationException("SystemOne TimeoutSeconds must be positive.");
        }

        var normalizedBaseUrl = BaseUrl.Trim().TrimEnd('/') + "/";
        return new Uri(new Uri(normalizedBaseUrl, UriKind.Absolute), "v1/systemone");
    }
}
