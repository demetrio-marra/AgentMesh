namespace AgentMesh.Application.Services.Pipelines
{
    public sealed class PluginHostState
    {
        private readonly object _sync = new();
        private readonly List<string> _startupDiagnostics = [];
        private readonly List<string> _startupErrors = [];
        private readonly List<string> _duplicatePipelineNames = [];

        public int LoadedPipelineCount { get; private set; }

        public IReadOnlyList<string> StartupDiagnostics
        {
            get
            {
                lock (_sync)
                {
                    return _startupDiagnostics.ToList();
                }
            }
        }

        public IReadOnlyList<string> StartupErrors
        {
            get
            {
                lock (_sync)
                {
                    return _startupErrors.ToList();
                }
            }
        }

        public IReadOnlyList<string> DuplicatePipelineNames
        {
            get
            {
                lock (_sync)
                {
                    return _duplicatePipelineNames.ToList();
                }
            }
        }

        public bool HasConfigurationIssues
        {
            get
            {
                lock (_sync)
                {
                    return _startupErrors.Count > 0 || _duplicatePipelineNames.Count > 0;
                }
            }
        }

        public void AddDiagnostic(string message)
        {
            lock (_sync)
            {
                _startupDiagnostics.Add(message);
            }
        }

        public void AddStartupError(string message)
        {
            lock (_sync)
            {
                _startupErrors.Add(message);
            }
        }

        public void SetPipelineValidationState(int loadedPipelineCount, IEnumerable<string> duplicatePipelineNames)
        {
            lock (_sync)
            {
                LoadedPipelineCount = loadedPipelineCount;
                _duplicatePipelineNames.Clear();
                _duplicatePipelineNames.AddRange(duplicatePipelineNames);
            }
        }
    }
}