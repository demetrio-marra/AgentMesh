using System.Text.Encodings.Web;
using AgentMesh.Runtime.Authentication;
using AgentMesh.Runtime.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AgentMesh.Runtime.Tests;

public class ApiKeyAuthenticationTests
{
    [Fact]
    public async Task ValidApiKeyAuthenticatesApiClient()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Api-Key"] = "secret";

        var result = await AuthenticateAsync(context, new ApiKeyAuthenticationConfiguration { ApiKey = "secret" });

        Assert.True(result.Succeeded);
        Assert.Equal("ApiClient", result.Principal!.Identity!.Name);
    }

    [Fact]
    public async Task MissingApiKeyFailsAuthentication()
    {
        var result = await AuthenticateAsync(new DefaultHttpContext(), new ApiKeyAuthenticationConfiguration { ApiKey = "secret" });

        Assert.False(result.Succeeded);
        Assert.Equal("Missing API key.", result.Failure!.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("wrong")]
    public async Task EmptyOrIncorrectApiKeyFailsAuthentication(string providedKey)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Api-Key"] = providedKey;

        var result = await AuthenticateAsync(context, new ApiKeyAuthenticationConfiguration { ApiKey = "secret" });

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid API key.", result.Failure!.Message);
    }

    private static async Task<AuthenticateResult> AuthenticateAsync(
        HttpContext context,
        ApiKeyAuthenticationConfiguration configuration)
    {
        var handler = new ApiKeyAuthenticationHandler(
            new FixedOptionsMonitor<AuthenticationSchemeOptions>(new AuthenticationSchemeOptions()),
            NullLoggerFactory.Instance,
            UrlEncoder.Default,
            configuration);
        var scheme = new AuthenticationScheme(ApiKeyAuthenticationDefaults.SchemeName, ApiKeyAuthenticationDefaults.SchemeName, typeof(ApiKeyAuthenticationHandler));

        await handler.InitializeAsync(scheme, context);
        return await handler.AuthenticateAsync();
    }

    private sealed class FixedOptionsMonitor<TOptions>(TOptions options) : IOptionsMonitor<TOptions>
        where TOptions : class
    {
        public TOptions CurrentValue => options;

        public TOptions Get(string? name) => options;

        public IDisposable? OnChange(Action<TOptions, string?> listener) => null;
    }
}