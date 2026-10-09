namespace AgentMesh.Application.Models
{
    public record struct ParameterStoreItem
    {
        public int Version { get; set; }
        public object? Value { get; set; }
    }
}
