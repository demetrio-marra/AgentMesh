namespace AgentMesh.Models.Api;

public sealed class SummarizationApiOutput
{
    public Guid RequestId { get; set; }

    public string SummarizedContent { get; set; } = string.Empty;

    public DateTime SummarizedContentDatetime { get; set; }
}