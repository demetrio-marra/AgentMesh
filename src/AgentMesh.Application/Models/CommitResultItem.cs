namespace AgentMesh.Application.Models
{
    public readonly record struct CommitResultItem(Type ParameterType,
        object? OldValue, 
        object? NewValue);
}
