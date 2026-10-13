namespace AgentMesh.Application.Models
{
    public interface IEWParameterConfiguration
    {
        public Type ValueType { get; }
        public string Name { get; }
        public IEWParameterSerializer DisplayValueSerializer { get; }
        public IEWParameterSerializer ValueSerializer { get; }
        public object? GetDefaultValue();
    }
}
