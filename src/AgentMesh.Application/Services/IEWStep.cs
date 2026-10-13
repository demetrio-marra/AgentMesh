namespace AgentMesh.Application.Services
{
    public interface IEWStep
    {
        string Name { get; }
        IEnumerable<Type> InputParameterTypes { get; }
        IEnumerable<Type> OutputParameterTypes { get; }
        Task<EWStepExecutionResult> ExecuteAsync(IReadOnlyDictionary<Type, object?> Values, CancellationToken cancellationToken = default);
    }
}
