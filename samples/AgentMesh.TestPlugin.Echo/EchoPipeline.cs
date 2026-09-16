using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.TestPlugin.Echo
{
    public sealed class EchoPipeline : IChatRequestPipeline
    {
        private string _message = string.Empty;

        public string Name => "Echo";
        public string FinalResponse => $"Echo:{_message}";

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
