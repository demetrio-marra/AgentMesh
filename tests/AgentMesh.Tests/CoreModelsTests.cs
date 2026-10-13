using System.Text.Json;
using AgentMesh.Exceptions;
using AgentMesh.Models;
using Xunit;

namespace AgentMesh.Tests;

public class CoreModelsTests
{
    [Fact]
    public void AgentExecutionCostCalculatesTokenAndHourlyCosts()
    {
        var tokenCost = new AgentExecutionCost("agent", 2m, 8m, 500_000, 250_000, null, TimeSpan.Zero);
        var hourlyCost = new AgentExecutionCost("agent", 0m, 0m, 0, 0, 12m, TimeSpan.FromMinutes(15));

        Assert.Equal(750_000, tokenCost.TotalTokens);
        Assert.Equal(3m, tokenCost.TotalCost);
        Assert.Equal(3m, hourlyCost.TotalCost);
    }

    [Fact]
    public void StepStatisticsCalculateElapsedTimeAndAgentTokens()
    {
        var started = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var statistics = new EWStepStatisticsRecord(
            "step", started, started.AddSeconds(65), [], [], [],
            IsAgentic: true, InputTokens: 12, OutputTokens: 8);

        Assert.Equal(TimeSpan.FromSeconds(65), statistics.Elapsed);
        Assert.Equal("1m 5s", statistics.HumanReadableElapsed);
        Assert.Equal(20, statistics.TotalTokens);
    }

    [Fact]
    public void ParameterDiffIncludesChangedAddedAndRemovedValues()
    {
        var statistics = new EWStepStatisticsRecord(
            "step", default, default,
            [new("changed", "old"), new("removed", "value")],
            [],
            [new("changed", "new"), new("added", "value")]);

        var differences = statistics.ParametersDiff.ToArray();

        Assert.Equal(3, differences.Length);
        Assert.Contains(differences, difference => difference == new EWDisplayDiffParameterRecord("changed", "old", "new"));
        Assert.Contains(differences, difference => difference == new EWDisplayDiffParameterRecord("removed", "value", null));
        Assert.Contains(differences, difference => difference == new EWDisplayDiffParameterRecord("added", null, "value"));
    }

    [Fact]
    public void ContextMessageRoleUsesStringJsonValues()
    {
        var serialized = JsonSerializer.Serialize(ContextMessageRole.Assistant);

        Assert.Equal("\"Assistant\"", serialized);
        Assert.Equal(ContextMessageRole.Assistant, JsonSerializer.Deserialize<ContextMessageRole>(serialized));
    }

    [Fact]
    public void ParameterMutationPreservesVersionTypeAndNullableValue()
    {
        var mutation = new ParameterMutation(4, typeof(string), null);

        Assert.Equal(4, mutation.ParameterVersion);
        Assert.Equal(typeof(string), mutation.ParameterType);
        Assert.Null(mutation.NewValue);
    }

    [Fact]
    public void RoutingFailuresExposeServiceUnavailableDetails()
    {
        var configurationFailure = PipelineRoutingException.PluginConfigurationInvalid();
        var unavailable = PipelineRoutingException.NoPipelinesLoaded();

        Assert.Equal(503, configurationFailure.StatusCode);
        Assert.Equal("Plugin configuration error", configurationFailure.Title);
        Assert.Equal(configurationFailure.Detail, configurationFailure.Message);
        Assert.Equal(503, unavailable.StatusCode);
        Assert.Equal("No pipelines loaded", unavailable.Detail);
    }
}