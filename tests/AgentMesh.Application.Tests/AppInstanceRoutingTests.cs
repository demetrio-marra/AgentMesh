using AgentMesh.Application.Services;
using AgentMesh.Contracts.Configuration;
using AgentMesh.Exceptions;
using AgentMesh.Infrastructure.JSSandbox;
using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AgentMesh.Application.Tests;

public class AppInstanceRoutingTests
{
    [Fact]
    public async Task RequestWithoutPipelineReturnsUnavailableRoutingError()
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        var appInstance = CreateAppInstance(services);

        var exception = await Assert.ThrowsAsync<PipelineRoutingException>(() =>
            appInstance.ProcessRequest("question", null));

        Assert.Equal(503, exception.StatusCode);
        Assert.Equal("Service unavailable", exception.Title);
        Assert.Equal("No pipelines loaded", exception.Detail);
    }

    [Fact]
    public async Task RequestWithMultiplePipelinesReturnsConfigurationError()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<IChatRequestPipeline>(_ => new EmptyChatPipeline());
        serviceCollection.AddScoped<IChatRequestPipeline>(_ => new EmptyChatPipeline());
        using var services = serviceCollection.BuildServiceProvider();
        var appInstance = CreateAppInstance(services);

        var exception = await Assert.ThrowsAsync<PipelineRoutingException>(() =>
            appInstance.ProcessRequest("question", null));

        Assert.Equal(503, exception.StatusCode);
        Assert.Equal("Plugin configuration error", exception.Title);
    }

    private static AppInstance CreateAppInstance(IServiceProvider services) => new(
        services,
        Array.Empty<AgentFlatConfigurationRecord>(),
        new SESJSSandboxConfiguration(),
        new UserConfiguration(),
        new UnusedHttpClientFactory(),
        NullLogger<AppInstance>.Instance);

    private sealed class EmptyChatPipeline : IChatRequestPipeline
    {
        public string FinalResponse => string.Empty;

        public void SetParameterInitialValues(string userLastRequest, IEnumerable<ContextMessage> initialChatHistory, DateTime requestDateTime)
        {
        }

        public Task<IEnumerable<EWStepStatisticsRecord>> ExecuteAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Enumerable.Empty<EWStepStatisticsRecord>());
    }

    private sealed class UnusedHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}