using AgentMeshCLI.Configuration;
using AgentMeshCLI.Helpers;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AgentMeshCLI.Tests;

public class ConsoleConfigurationTests
{
    [Fact]
    public void ApiConfigurationBindsFromInMemorySettings()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Api:BaseUrl"] = "https://api.test/",
                ["Api:ApiKey"] = "test-key",
                ["Api:HeaderName"] = "X-Test-Key"
            })
            .Build();

        var settings = configuration.GetSection(ApiClientConfiguration.SectionName).Get<ApiClientConfiguration>();

        Assert.NotNull(settings);
        Assert.Equal("https://api.test/", settings.BaseUrl);
        Assert.Equal("test-key", settings.ApiKey);
        Assert.Equal("X-Test-Key", settings.HeaderName);
    }

    [Theory]
    [InlineData(0, "0s")]
    [InlineData(0.5, "<1s")]
    [InlineData(60, "1m0s")]
    [InlineData(3661, "1h1m1s")]
    public void DurationFormattingIsStable(double seconds, string expected)
    {
        Assert.Equal(expected, ConsoleHelper.ToHumanReadableDuration(TimeSpan.FromSeconds(seconds)));
    }

}