using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.TestPlugin.DuplicateEcho
{
    public sealed class DuplicateEchoPipeline : IChatRequestPipeline
    {
        private string _message = string.Empty;

        public string Name => "echo";
        public string FinalResponse => $"DuplicateEcho:{_message}";

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
