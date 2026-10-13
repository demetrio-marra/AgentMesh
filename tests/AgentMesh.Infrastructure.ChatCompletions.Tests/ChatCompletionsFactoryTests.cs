using AgentMesh.Contracts;
using AgentMesh.Contracts.Configuration;
using AgentMesh.Infrastructure.ChatCompletions;
using Xunit;

namespace AgentMesh.Infrastructure.ChatCompletions.Tests;

public class ChatCompletionsFactoryTests
{
    [Fact]
    public void FactoryResolvesRoleWithoutCaseSensitivity()
    {
        var factory = new ChatCompletionsClientFactory([CreateConfiguration("FunctionalAnalyst")]);

        var client = factory.CreateChatClient("functionalanalyst");

        Assert.IsType<ChatCompletionsClient>(client);
    }

    [Fact]
    public void FactoryRejectsUnknownRole()
    {
        var factory = new ChatCompletionsClientFactory([CreateConfiguration("KnownAgent")]);

        Assert.Throws<InvalidOperationException>(() => factory.CreateChatClient("UnknownAgent"));
    }

    [Fact]
    public void ClientRejectsNullRequiredConfiguration()
    {
        Assert.Throws<ArgumentNullException>(() => new ChatCompletionsClient(
            null!, "key", "https://provider.test/v1", "0.5", "prompt"));
        Assert.Throws<ArgumentNullException>(() => new ChatCompletionsClient(
            "model", "key", "https://provider.test/v1", "0.5", null!));
    }

    [Fact]
    public void ClientRejectsInvalidTemperature()
    {
        Assert.Throws<FormatException>(() => new ChatCompletionsClient(
            "model", "key", "https://provider.test/v1", "not-a-number", "prompt"));
    }

    private static AgentFlatConfigurationRecord CreateConfiguration(string role) => new()
    {
        AgentUniqueRole = role,
        ProviderName = "test-provider",
        ProviderEndpoint = "https://provider.test/v1",
        ProviderApiKey = "test-key",
        ProviderModelName = "test-model",
        LLMClass = "test-model-class",
        Temperature = "0.5",
        SystemPrompt = "system prompt"
    };
}