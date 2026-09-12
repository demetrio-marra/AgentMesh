using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.TestPlugin.Support
{
    public sealed class SupportPipeline : IChatRequestPipeline
    {
        private string _message = string.Empty;

        public string Name => "Support";
        public string FinalResponse => $"Support:{_message}";

        public Task<IEnumerable<EWStepStatisticsRecord>> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            IEnumerable<EWStepStatisticsRecord> steps = [];
            return Task.FromResult(steps);
        }

        public void SetParameterInitialValues(string userLastRequest, IEnumerable<ContextMessage> initialChatHistory, DateTime requestDateTime)
        {
            _message = userLastRequest;
        }
    }
}
