namespace AgentMesh.Configuration
{
    public interface IConversationSummarizationSettings
    {
        int SummaryTokenThreshold { get; }
        int NumMessageToPreseve { get; }
        string SummarizeLanguage { get; }
    }
}