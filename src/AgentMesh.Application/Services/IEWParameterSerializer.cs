namespace AgentMesh.Application.Services
{
    public interface IEWParameterSerializer
    {
        string Serialize<T>(T obj);
    }
}
