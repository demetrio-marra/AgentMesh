namespace AgentMesh.Models
{
    /// <summary>
    /// Represents execution statistics, token counts, and parameter changes for a single pipeline step.
    /// </summary>
    /// <param name="StepName">The name of the executed step.</param>
    /// <param name="StartedOnUtc">Timestamp in UTC when step execution started.</param>
    /// <param name="CompletedOnUtc">Timestamp in UTC when step execution finished.</param>
    /// <param name="ParametersBefore">Parameter values prior to step execution.</param>
    /// <param name="InputParameters">Parameters consumed as input by the step.</param>
    /// <param name="ParametersAfter">Parameter values after step execution and mutation commit.</param>
    /// <param name="IsAgentic">Indicates whether the step invoked an AI agent.</param>
    /// <param name="AgentName">The name of the AI agent if agentic, otherwise null.</param>
    /// <param name="CountInputTokensAsContextTokens">Whether input tokens count towards conversation context size.</param>
    /// <param name="CountOutputTokensAsContextTokens">Whether output tokens count towards conversation context size.</param>
    /// <param name="InputTokens">Number of prompt/input tokens consumed.</param>
    /// <param name="OutputTokens">Number of completion/output tokens generated.</param>
    public record struct EWStepStatisticsRecord(string StepName,
        DateTime StartedOnUtc,
        DateTime CompletedOnUtc,
        IEnumerable<EWDisplayParameterRecord> ParametersBefore,
        IEnumerable<EWDisplayParameterRecord> InputParameters,
        IEnumerable<EWDisplayParameterRecord> ParametersAfter,
        bool IsAgentic = false,
        string? AgentName = null,
        bool CountInputTokensAsContextTokens = false,
        bool CountOutputTokensAsContextTokens = false,
        int? InputTokens = null,
        int? OutputTokens = null)
    {

        public readonly TimeSpan Elapsed { get => CompletedOnUtc - StartedOnUtc; }
        public readonly int? TotalTokens { get => !IsAgentic ? null : (InputTokens ?? 0) + (OutputTokens ?? 0); }

        public readonly string HumanReadableElapsed
        {
            get
            {
                var elapsed = Elapsed;
                var elapsedParts = new List<string>();

                if (elapsed.Hours > 0)
                {
                    elapsedParts.Add($"{elapsed.Hours}h");
                }
                if (elapsed.Minutes > 0)
                {
                    elapsedParts.Add($"{elapsed.Minutes}m");
                }
                if (elapsed.Seconds > 0)
                {
                    elapsedParts.Add($"{elapsed.Seconds}s");
                }
                
                if (elapsedParts.Count == 0)
                {
                    return "<1s";
                }

                return string.Join(" ", elapsedParts);
            }
        }

        public readonly IEnumerable<EWDisplayDiffParameterRecord> ParametersDiff
        {
            get
            {
                var beforeDict = ParametersBefore.ToDictionary(p => p.Name);
                var afterDict = ParametersAfter.ToDictionary(p => p.Name);
                var allKeys = new HashSet<string>(beforeDict.Keys.Concat(afterDict.Keys));
                foreach (var key in allKeys)
                {
                    beforeDict.TryGetValue(key, out var beforeParam);
                    afterDict.TryGetValue(key, out var afterParam);
                    if ((beforeParam.Value ?? string.Empty) != (afterParam.Value ?? string.Empty))
                    {
                        yield return new EWDisplayDiffParameterRecord(
                            Name: key,
                            OldValue: beforeParam.Value,
                            NewValue: afterParam.Value
                        );
                    }
                }
            }

        }
    }
}
