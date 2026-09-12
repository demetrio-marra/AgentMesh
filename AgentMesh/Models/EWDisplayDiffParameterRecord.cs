namespace AgentMesh.Models
{
    /// <summary>
    /// Represents the difference in a parameter's value before and after step execution.
    /// </summary>
    /// <param name="Name">The parameter name.</param>
    /// <param name="OldValue">The previous parameter value, or null if unset.</param>
    /// <param name="NewValue">The updated parameter value, or null if unset.</param>
    public record struct EWDisplayDiffParameterRecord(string Name,
        string? OldValue, string? NewValue);
}
