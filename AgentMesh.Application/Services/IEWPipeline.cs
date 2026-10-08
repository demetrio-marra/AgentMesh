namespace AgentMesh.Application.Services
{
    public interface IEWPipeline
    {
        Task<IEnumerable<EWStepStatisticsRecord>> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}