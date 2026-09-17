using AgentMesh.Models.Costs;

namespace AgentMesh.Models.Workflows
{
    /// <summary>
    /// Represents the comprehensive result produced by executing a pipeline workflow.
    /// </summary>
    public readonly record struct WorkflowResult
    {
        /// <summary>
        /// The final textual response generated for the user request.
        /// </summary>
        public string Message { get; init; }

        /// <summary>
        /// Execution statistics, timing, and parameter diffs for each step executed in the main pipeline.
        /// </summary>
        public IEnumerable<EWStepStatisticsRecord> MainPipelineStepsData { get; init; }

        /// <summary>
        /// Itemized execution costs and token usage for all AI agents invoked during the workflow.
        /// </summary>
        public IEnumerable<AgentExecutionCost> AgentsCostData { get; init; }

        /// <summary>
        /// Total number of messages present in the conversation context.
        /// </summary>
        public int CountOfMessages { get; init; }

        /// <summary>
        /// Total count of context tokens tracked across the conversation.
        /// </summary>
        public int CountOfTokens { get; init; }

        /// <summary>
        /// Indicates whether the conversation summarization pipeline was executed during this request.
        /// </summary>
        public bool ContextSummarizerHasRun { get; init; }

        /// <summary>
        /// Number of messages in context prior to summarization (null if summarization did not run).
        /// </summary>
        public int? CountOfMessagesBeforeSummarization { get; init; }

        /// <summary>
        /// Token count in context prior to summarization (null if summarization did not run).
        /// </summary>
        public int? CountOfTokensBeforeSummarization { get; init; }

        /// <summary>
        /// Cumulated financial cost in USD for this execution (or entire conversation session in interactive mode).
        /// </summary>
        public decimal CumulatedCost { get; init; }
    }
}
