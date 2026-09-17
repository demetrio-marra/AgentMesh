namespace AgentMesh.Models.Workflows;

/// <summary>
/// Result produced by executing a summarization pipeline.
/// </summary>
/// <param name="SummarizedContent">The summarized conversation content.</param>
/// <param name="SummarizedContentDatetime">The timestamp of the summarized content.</param>
public sealed record SummarizationResult(string SummarizedContent, DateTime SummarizedContentDatetime);
