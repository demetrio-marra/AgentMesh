using System.ComponentModel.DataAnnotations;
using AgentMesh.Models;

namespace AgentMesh.Models.Api;

public sealed class SummarizationAsyncApiInput : IValidatableObject
{
    [Required]
    public string SummarizationLanguage { get; set; } = string.Empty;

    [Required]
    public IEnumerable<ContextMessage>? Conversation { get; set; }

    public string? WorkflowStartedCallbackUrl { get; set; }

    public string? WorkflowStepStartedCallbackUrl { get; set; }

    public string? WorkflowStepCompletedCallbackUrl { get; set; }

    public string? WorkflowCompletedCallbackUrl { get; set; }

    public string? WorkflowErrorCallbackUrl { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var callbackUrls = new[]
        {
            WorkflowStartedCallbackUrl,
            WorkflowStepStartedCallbackUrl,
            WorkflowStepCompletedCallbackUrl,
            WorkflowCompletedCallbackUrl,
            WorkflowErrorCallbackUrl
        };

        var suppliedCount = callbackUrls.Count(url => !string.IsNullOrWhiteSpace(url));

        if (suppliedCount != 0 && suppliedCount != callbackUrls.Length)
        {
            yield return new ValidationResult(
                "All 5 summarization callback URLs must be supplied together, or none at all.",
                [
                    nameof(WorkflowStartedCallbackUrl),
                    nameof(WorkflowStepStartedCallbackUrl),
                    nameof(WorkflowStepCompletedCallbackUrl),
                    nameof(WorkflowCompletedCallbackUrl),
                    nameof(WorkflowErrorCallbackUrl)
                ]);
        }
    }
}