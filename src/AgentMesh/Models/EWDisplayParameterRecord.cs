namespace AgentMesh.Models
{
    /// <summary>
    /// Represents a parameter name and its formatted display value.
    /// </summary>
    /// <param name="Name">The unique parameter name.</param>
    /// <param name="Value">The string representation of the parameter value.</param>
    public record struct EWDisplayParameterRecord(string Name, string Value);
}
