using System.Collections.ObjectModel;

namespace AgentMesh.Contracts.Models.SystemOne;

public sealed class SystemOneInput
{
    public SystemOneInput(SystemOneJsonValue state, IReadOnlyDictionary<string, SystemOneQuestion> questions)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(questions);

        State = new SystemOneJsonValue(state.Value);
        Questions = new ReadOnlyDictionary<string, SystemOneQuestion>(
            questions.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));
    }

    public SystemOneJsonValue State { get; }

    public IReadOnlyDictionary<string, SystemOneQuestion> Questions { get; }
}
