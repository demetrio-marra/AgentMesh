namespace AgentMeshWeb.Models
{
    public sealed class ChatPageViewModel
    {
        public ConfigurationSummaryApiOutput? Configuration { get; init; }
        public string? ConfigurationError { get; init; }
    }
}