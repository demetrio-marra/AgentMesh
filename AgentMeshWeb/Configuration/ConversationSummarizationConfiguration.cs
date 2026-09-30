using System.ComponentModel.DataAnnotations;

namespace AgentMeshWeb.Configuration;

public sealed class ConversationSummarizationConfiguration
{
    public const string SectionName = "ConversationSummarization";

    [Range(1, int.MaxValue)]
    public int SummaryTokenThreshold { get; set; } = 4000;

    [Range(1, int.MaxValue)]
    public int NumMessageToPreseve { get; set; } = 6;

    [Required]
    public string SummarizeLanguage { get; set; } = "English";
}