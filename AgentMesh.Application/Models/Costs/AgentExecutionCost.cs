namespace AgentMesh.Application.Models.Costs
{
    /// <summary>
    /// Itemized financial cost and token breakdown for an AI agent's execution.
    /// </summary>
    /// <param name="AgentName">The identifier or role name of the AI agent.</param>
    /// <param name="CostPerMillionInputTokens">Cost in USD per 1M prompt/input tokens.</param>
    /// <param name="CostPerMillionOutputTokens">Cost in USD per 1M completion/output tokens.</param>
    /// <param name="ConsumedInputTokens">Number of prompt/input tokens consumed by this agent.</param>
    /// <param name="ConsumedOutputTokens">Number of completion/output tokens produced by this agent.</param>
    /// <param name="CostPerHour">Hourly cost in USD if model pricing is time-based, otherwise null.</param>
    /// <param name="Elapsed">Execution duration spent by this agent.</param>
    public readonly record struct AgentExecutionCost(string AgentName,
        decimal CostPerMillionInputTokens,
        decimal CostPerMillionOutputTokens,
        int ConsumedInputTokens,
        int ConsumedOutputTokens,
        decimal? CostPerHour,
        TimeSpan Elapsed)
    {
        public readonly bool IsHourlyCost => CostPerHour.HasValue;
        public readonly decimal InputCost => IsHourlyCost ? 0 : ConsumedInputTokens / 1_000_000m * CostPerMillionInputTokens;
        public readonly decimal OutputCost => IsHourlyCost ? 0 : ConsumedOutputTokens / 1_000_000m * CostPerMillionOutputTokens;
        public readonly decimal HourlyCost => !IsHourlyCost ? 0 : (decimal)Elapsed.TotalHours * CostPerHour!.Value;
        public readonly decimal TotalCost => IsHourlyCost ? HourlyCost : InputCost + OutputCost;
        public readonly int TotalTokens => ConsumedInputTokens + ConsumedOutputTokens;
    }
}
