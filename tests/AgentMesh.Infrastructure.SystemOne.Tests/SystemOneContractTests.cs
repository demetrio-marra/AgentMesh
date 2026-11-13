using System.Text.Json;
using AgentMesh.Contracts.Models.SystemOne;
using Xunit;

namespace AgentMesh.Infrastructure.SystemOne.Tests;

public class SystemOneContractTests
{
    [Fact]
    public void InputOwnsStructuredJsonAfterSourceDocumentsAreDisposed()
    {
        SystemOneJsonValue state;
        SystemOneJsonValue instructions;

        using (var stateDocument = System.Text.Json.JsonDocument.Parse("""{"message":["hello",{"value":2}]}"""))
        using (var instructionsDocument = System.Text.Json.JsonDocument.Parse("""{"rule":"retain"}"""))
        {
            state = new SystemOneJsonValue(stateDocument.RootElement);
            instructions = new SystemOneJsonValue(instructionsDocument.RootElement);
        }

        var input = new SystemOneInput(
            state,
            new Dictionary<string, SystemOneQuestion>
            {
                ["decisionKey"] = new SystemOneNoulQuestion { Instructions = instructions }
            });

        Assert.Equal("hello", input.State.Value.GetProperty("message")[0].GetString());
        var question = Assert.IsType<SystemOneNoulQuestion>(input.Questions["decisionKey"]);
        Assert.Equal("retain", question.Instructions!.Value.GetProperty("rule").GetString());
    }

    [Fact]
    public void ContractSupportsTypedQuestionsAnswersAndUsage()
    {
        var input = new SystemOneInput(
            SystemOneJsonValue.From("text"),
            new Dictionary<string, SystemOneQuestion>
            {
                ["noul"] = new SystemOneNoulQuestion(),
                ["choice"] = new SystemOneChoiceQuestion
                {
                    Criteria = new Dictionary<string, SystemOneJsonValue>
                    {
                        ["userChoiceKey"] = SystemOneJsonValue.Null
                    }
                },
                ["score"] = new SystemOneScoreQuestion
                {
                    Criteria = [SystemOneJsonValue.From("low"), SystemOneJsonValue.From("high")]
                }
            });
        var result = new SystemOneResult(
            "jev-1.14.0",
            new Dictionary<string, SystemOneAnswer>
            {
                ["noul"] = new SystemOneNoulAnswer(0.75),
                ["choice"] = new SystemOneChoiceAnswer(
                    "userChoiceKey",
                    0.8,
                    new Dictionary<string, double> { ["userChoiceKey"] = 1 }),
                ["score"] = new SystemOneScoreAnswer(
                    0.5,
                    0.9,
                    new Dictionary<string, SystemOneJsonValue>(),
                    new Dictionary<string, double> { ["0"] = 0.5, ["1"] = 0.5 })
            },
            new SystemOneUsage(12, 3));

        Assert.Equal(JsonValueKind.String, input.State.Value.ValueKind);
        Assert.Equal(3, input.Questions.Count);
        Assert.Equal("jev-1.14.0", result.Model);
        Assert.Equal(12, result.Usage.InputTokens);
        Assert.Equal(3, result.Usage.OutputTokens);
    }
}
