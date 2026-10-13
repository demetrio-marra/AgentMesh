using AgentMesh.Application.Models;
using AgentMesh.Application.Services;
using AgentMesh.Models;
using AgentMesh.Services;
using Xunit;

namespace AgentMesh.Application.Tests;

public class ParameterStoreTests
{
    [Fact]
    public void SnapshotStartsAtDefaultAndInitialValuesResetVersion()
    {
        var store = CreateStore();
        var initialSnapshot = store.CreateSnapshot([typeof(CountParameter)]);

        Assert.Equal(1, initialSnapshot.Values[typeof(CountParameter)].Version);
        Assert.Equal(2, initialSnapshot.Values[typeof(CountParameter)].Value);

        store.SetInitialValues(new Dictionary<Type, object?> { [typeof(CountParameter)] = 5 });
        var resetSnapshot = store.CreateSnapshot([typeof(CountParameter)]);

        Assert.Equal(1, resetSnapshot.Values[typeof(CountParameter)].Version);
        Assert.Equal(5, resetSnapshot.Values[typeof(CountParameter)].Value);
    }

    [Fact]
    public void UnknownParameterTypesAreRejected()
    {
        var store = CreateStore();

        Assert.Throws<ArgumentException>(() => store.SetInitialValues(
            new Dictionary<Type, object?> { [typeof(string)] = "unregistered" }));
        Assert.Throws<KeyNotFoundException>(() => store.CreateSnapshot([typeof(string)]));
    }

    [Fact]
    public void CommitChangesValueAndAdvancesSnapshotVersion()
    {
        var store = CreateStore();

        var result = store.TryCommit("increment", [new ParameterMutation(1, typeof(CountParameter), 3)]);
        var snapshot = store.CreateSnapshot([typeof(CountParameter)]);

        var committed = Assert.Single(result);
        Assert.Equal(2, committed.OldValue);
        Assert.Equal(3, committed.NewValue);
        Assert.Equal(2, snapshot.Values[typeof(CountParameter)].Version);
        Assert.Equal(3, snapshot.Values[typeof(CountParameter)].Value);
    }

    [Fact]
    public void StaleBatchCommitDoesNotApplyAnyMutation()
    {
        var store = CreateStore();
        var mutations = new ParameterMutation[]
        {
            new(1, typeof(CountParameter), 9),
            new(0, typeof(LabelParameter), "stale")
        };

        var exception = Assert.Throws<InvalidOperationException>(() => store.TryCommit("stale-step", mutations));
        var snapshot = store.CreateSnapshot([typeof(CountParameter), typeof(LabelParameter)]);

        Assert.Contains("stale-step", exception.Message);
        Assert.Equal(2, snapshot.Values[typeof(CountParameter)].Value);
        Assert.Equal(1, snapshot.Values[typeof(CountParameter)].Version);
    }

    [Fact]
    public void ParameterValueAsReturnsTypedValueOrDefault()
    {
        var parameter = new CountParameter();

        Assert.Equal(7, parameter.ValueAs(7));
        Assert.Equal(0, parameter.ValueAs("7"));
    }

    private static ParameterStore CreateStore() => new(new IEWParameterConfiguration[]
    {
        new CountParameter(),
        new LabelParameter()
    });

    private sealed class CountParameter : BaseEWParameterConfiguration<int>
    {
        public override string Name => "Count";

        protected override int GetDefaultValue() => 2;
    }

    private sealed class LabelParameter : BaseEWParameterConfiguration<string>
    {
        public override string Name => "Label";

        protected override string GetDefaultValue() => "initial";
    }
}

public class EWPipelineTests
{
    [Fact]
    public async Task StepMutationIsCommittedAndReportedInStatistics()
    {
        var parameter = new PipelineParameter();
        var store = new ParameterStore([parameter]);
        var notifier = new RecordingProgressNotifier();
        var step = new MutationStep(typeof(PipelineParameter), 8);
        var pipeline = new SingleStepPipeline(notifier, store, [parameter], step);

        var statistics = Assert.Single(await pipeline.ExecuteAsync());
        var snapshot = store.CreateSnapshot([typeof(PipelineParameter)]);

        Assert.Equal(8, snapshot.Values[typeof(PipelineParameter)].Value);
        Assert.Equal(2, snapshot.Values[typeof(PipelineParameter)].Version);
        Assert.Equal("3", Assert.Single(statistics.ParametersBefore).Value);
        Assert.Equal("8", Assert.Single(statistics.ParametersAfter).Value);
        Assert.Equal("PipelineValue", Assert.Single(statistics.ParametersDiff).Name);
        Assert.Equal(1, notifier.CompletedSteps);
    }

    private sealed class PipelineParameter : BaseEWParameterConfiguration<int>
    {
        public override string Name => "PipelineValue";

        protected override int GetDefaultValue() => 3;
    }

    private sealed class MutationStep(Type parameterType, int value) : IEWStep
    {
        public string Name => "Mutation";
        public IEnumerable<Type> InputParameterTypes => [];
        public IEnumerable<Type> OutputParameterTypes => [parameterType];

        public Task<EWStepExecutionResult> ExecuteAsync(
            IReadOnlyDictionary<Type, object?> Values,
            CancellationToken cancellationToken = default) => Task.FromResult(new EWStepExecutionResult
            {
                OutputMutations = new Dictionary<Type, object?> { [parameterType] = value }
            });
    }

    private sealed class SingleStepPipeline(
        IWorkflowProgressNotifier notifier,
        IParameterStore store,
        IEnumerable<IEWParameterConfiguration> configurations,
        IEWStep step) : EWPipeline(notifier, store, configurations)
    {
        private bool _scheduled;

        protected override IEnumerable<IEWStep> GetNextStepsToRun()
        {
            if (_scheduled)
            {
                return [];
            }

            _scheduled = true;
            return [step];
        }
    }

    private sealed class RecordingProgressNotifier : IWorkflowProgressNotifier
    {
        public int CompletedSteps { get; private set; }

        public Task NotifyWorkflowStart() => Task.CompletedTask;
        public Task NotifyWorkflowEnd() => Task.CompletedTask;
        public Task NotifyWorkflowStepStarted(string stepName, IEnumerable<EWDisplayParameterRecord> inputParameters) => Task.CompletedTask;

        public Task NotifyWorkflowStepCompleted(string stepName, EWStepStatisticsRecord statistics)
        {
            CompletedSteps++;
            return Task.CompletedTask;
        }
    }
}