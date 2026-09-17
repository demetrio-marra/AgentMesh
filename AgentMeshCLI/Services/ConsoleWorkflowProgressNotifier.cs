using AgentMesh.Helpers;
using AgentMesh.Models;

namespace AgentMesh.Services;

internal sealed class ConsoleWorkflowProgressNotifier
{
    public Task NotifyWorkflowStart()
    {
        ConsoleHelper.WriteLineWithColor("\nWorkflow has started.", ConsoleColor.Gray);
        return Task.CompletedTask;
    }

    public Task NotifyWorkflowStepStarted(string stepName, IEnumerable<EWDisplayParameterRecord> inputParameters)
    {
        ConsoleHelper.WriteLineWithColor($"Workflow step '{stepName}' is running...", ConsoleColor.DarkGray);
        WriteParameters("Input Params", inputParameters.Select(p => new TextOrConsoleColor { Color = ConsoleColor.DarkGray, Text = $"{p.Name}: {p.Value}" }));
        return Task.CompletedTask;
    }

    public Task NotifyWorkflowStepCompleted(string stepName, TimeSpan elapsed, bool isAgentic, IEnumerable<EWDisplayDiffParameterRecord> parametersDiff)
    {
        ConsoleHelper.WriteLineWithColor($"Workflow step '{stepName}' has completed.", ConsoleColor.Yellow);
        WriteExecStatistic("Step", stepName + " " + (isAgentic ? "(Agentic)" : "(Code)"));
        WriteExecStatistic("Elapsed", elapsed.TotalSeconds < 1 ? "<1s" : elapsed.ToString());
        var differences = parametersDiff.ToList();
        var display = differences.Count == 0
            ? [new TextOrConsoleColor { Color = ConsoleColor.White, Text = "(No differences)" }]
            : differences.SelectMany(p => new[]
            {
                new TextOrConsoleColor { Color = ConsoleColor.White, Text = p.Name },
                new TextOrConsoleColor { Color = ConsoleColor.Magenta, Text = p.OldValue ?? string.Empty },
                new TextOrConsoleColor { Color = ConsoleColor.Green, Text = p.NewValue ?? string.Empty },
                new TextOrConsoleColor { Color = ConsoleColor.Gray, Text = new string('-', 9) }
            }).ToList();
        WriteParameters("Params changes", display);
        ConsoleHelper.WriteLineWithColor("Workflow step details displayed.", ConsoleColor.Yellow);
        return Task.CompletedTask;
    }

    private static void WriteParameters(string title, IEnumerable<TextOrConsoleColor> elements)
    {
        var padding = title.Length + 2;
        ConsoleHelper.WriteWithColor($"{title}: ", ConsoleColor.DarkYellow);
        var first = true;
        foreach (var element in elements)
        {
            foreach (var line in (element.Text ?? string.Empty).Split('\n'))
            {
                ConsoleHelper.WriteLineWithColor(first ? line : new string(' ', padding) + line, element.Color ?? ConsoleColor.White);
                first = false;
            }
        }
    }

    private static void WriteExecStatistic(string key, string value)
    {
        ConsoleHelper.WriteLineWithColor($"{key}: {value}", ConsoleColor.White);
    }

    private sealed class TextOrConsoleColor
    {
        public string? Text { get; init; }
        public ConsoleColor? Color { get; init; }
    }
}
