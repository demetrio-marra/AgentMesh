using System.ComponentModel.DataAnnotations;
using AgentMesh.Runtime.Configuration;
using AgentMesh.Runtime.Models;
using AgentMesh.Runtime.Models.Api;
using Xunit;

namespace AgentMesh.Runtime.Tests;

public class RuntimeApiModelTests
{
    [Fact]
    public void AsyncRequestAcceptsNoCallbacksOrAllCallbacks()
    {
        var noCallbacks = new ProcessRequestAsyncApiInput { Message = "question" };
        var allCallbacks = CreateRequestWithCallbacks();

        Assert.Empty(Validate(noCallbacks));
        Assert.Empty(Validate(allCallbacks));
    }

    [Fact]
    public void AsyncRequestRejectsPartialCallbackConfiguration()
    {
        var request = new ProcessRequestAsyncApiInput
        {
            Message = "question",
            WorkflowStartedCallbackUrl = "https://callbacks.test/started"
        };

        var validationResult = Assert.Single(Validate(request));

        Assert.Contains("All 5 callback URLs", validationResult.ErrorMessage);
        Assert.Equal(5, validationResult.MemberNames.Count());
    }

    [Fact]
    public void RequestInputRequiresMessage()
    {
        var input = new ProcessRequestApiInput();

        Assert.Contains(Validate(input), result => result.MemberNames.Contains(nameof(input.Message)));
    }

    [Fact]
    public void ApiKeyConfigurationHasStableDefaults()
    {
        var configuration = new ApiKeyAuthenticationConfiguration();

        Assert.Equal("ApiAuth", ApiKeyAuthenticationConfiguration.SectionName);
        Assert.Equal(string.Empty, configuration.ApiKey);
        Assert.Equal("X-Api-Key", configuration.HeaderName);
    }

    [Fact]
    public void CallbackPayloadDefaultsAreSafeForSerialization()
    {
        var payload = new WorkflowStepStartedCallbackPayload();

        Assert.Equal(Guid.Empty, payload.RequestId);
        Assert.Equal(string.Empty, payload.StepName);
    }

    private static ProcessRequestAsyncApiInput CreateRequestWithCallbacks() => new()
    {
        Message = "question",
        WorkflowStartedCallbackUrl = "https://callbacks.test/started",
        WorkflowStepStartedCallbackUrl = "https://callbacks.test/step-started",
        WorkflowStepCompletedCallbackUrl = "https://callbacks.test/step-completed",
        WorkflowCompletedCallbackUrl = "https://callbacks.test/completed",
        WorkflowErrorCallbackUrl = "https://callbacks.test/error"
    };

    private static List<ValidationResult> Validate(object instance)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
        return results;
    }
}