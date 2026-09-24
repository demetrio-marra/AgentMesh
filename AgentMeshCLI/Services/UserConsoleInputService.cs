using AgentMesh.Configuration;
using AgentMesh.Helpers;
using AgentMesh.Models;
using Microsoft.Extensions.Hosting;

namespace AgentMesh.Services;

internal sealed class UserConsoleInputService(
    AgentMeshApiClient apiClient,
    ConversationSummarizationConfiguration summarizationConfiguration,
    PendingRequestRegistry pendingRequests,
    ConversationState conversationState) : BackgroundService
{
    private bool _isFirstRun = true;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Welcome to AgentMesh! This is a console application that allows you to interact with the AgentMesh system.\n");
        await PrintConfigurationsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            Console.WriteLine("Enter your question or type /help:");
            Console.Write("> ");
            var requestText = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(requestText))
            {
                Console.WriteLine("Please enter a valid question.");
                continue;
            }

            if (string.Equals(requestText.Trim(), "/exit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            if (string.Equals(requestText.Trim(), "/help", StringComparison.OrdinalIgnoreCase))
            {
                PrintHelp();
                continue;
            }

            if (string.Equals(requestText.Trim(), "/new", StringComparison.OrdinalIgnoreCase))
            {
                conversationState.Reset();
                ConsoleHelper.WriteLineWithColor("New conversation initialized.", ConsoleColor.Green);
                continue;
            }

            if (string.Equals(requestText.Trim(), "/summarize", StringComparison.OrdinalIgnoreCase))
            {
                await SummarizeConversationAsync(stoppingToken, automatic: false);
                continue;
            }

            if (_isFirstRun)
            {
                ConsoleHelper.WriteLineWithColor("You can cancel the current request by pressing Ctrl+C.\n", ConsoleColor.Yellow);
                _isFirstRun = false;
            }

            await ProcessRequestAsync(requestText, stoppingToken);
        }
    }

    private async Task ProcessRequestAsync(string message, CancellationToken applicationCancellationToken)
    {
        var previousTreatControlCAsInput = Console.TreatControlCAsInput;
        Console.TreatControlCAsInput = true;
        using var requestCancellation = CancellationTokenSource.CreateLinkedTokenSource(applicationCancellationToken);
        var cancelMonitorTask = MonitorRequestCancellationByKeyboardAsync(requestCancellation, applicationCancellationToken);
        Guid? requestId = null;
        try
        {
            requestId = await apiClient.SubmitChatAsync(message, conversationState.Conversation, requestCancellation.Token);
            var pendingRequest = pendingRequests.Register(requestId.Value, isSummarization: false);
            var callbackResult = await pendingRequest.Completion.Task.WaitAsync(requestCancellation.Token);

            if (!string.IsNullOrWhiteSpace(callbackResult.ErrorMessage))
            {
                ConsoleHelper.WriteLineWithColor(callbackResult.ErrorMessage, ConsoleColor.Red);
                return;
            }

            var result = callbackResult.WorkflowResult ?? throw new InvalidOperationException("The API returned no workflow result.");
            var requestDate = DateTime.UtcNow;
            conversationState.Conversation.Add(new ContextMessage { Role = ContextMessageRole.User, Date = requestDate, Text = message });
            conversationState.Conversation.Add(new ContextMessage { Role = ContextMessageRole.Assistant, Date = DateTime.UtcNow, Text = result.Message });
            conversationState.TokensCount = result.CountOfTokens;
            conversationState.CumulatedCost += result.CumulatedCost;

            ConsoleHelper.WriteLineWithColor("\nResponse for user:", ConsoleColor.Gray);
            ConsoleHelper.WriteLineWithColor(result.Message, ConsoleColor.Cyan);
            PrintConversationStatus();
            ConsoleHelper.PrintTokenUsageSummary(result.MainPipelineStepsData, result.AgentsCostData);

            if (conversationState.TokensCount > summarizationConfiguration.SummaryTokenThreshold &&
                conversationState.Conversation.Count > summarizationConfiguration.NumMessageToPreseve)
            {
                await SummarizeConversationAsync(requestCancellation.Token, automatic: true);
            }
        }
        catch (OperationCanceledException) when (requestCancellation.IsCancellationRequested && !applicationCancellationToken.IsCancellationRequested)
        {
            if (requestId.HasValue)
            {
                pendingRequests.TryAbandon(requestId.Value);
            }

            ConsoleHelper.WriteLineWithColor("Request canceled.", ConsoleColor.Yellow);
        }
        catch (Exception exception)
        {
            ConsoleHelper.WriteLineWithColor(exception.Message, ConsoleColor.Red);
        }
        finally
        {
            requestCancellation.Cancel();
            await cancelMonitorTask;
            Console.TreatControlCAsInput = previousTreatControlCAsInput;
        }
    }

    private async Task SummarizeConversationAsync(CancellationToken cancellationToken, bool automatic)
    {
        var countBefore = conversationState.Conversation.Count;
        var preserveCount = Math.Min(countBefore, summarizationConfiguration.NumMessageToPreseve);
        var includeCount = countBefore - preserveCount;
        if (includeCount <= 0)
        {
            if (!automatic)
            {
                ConsoleHelper.WriteLineWithColor("There are not enough messages to summarize.", ConsoleColor.Yellow);
            }

            return;
        }

        Guid? requestId = null;
        try
        {
            var messagesToSummarize = conversationState.Conversation.Take(includeCount).ToList();
            requestId = await apiClient.SubmitSummarizationAsync(summarizationConfiguration.SummarizeLanguage, messagesToSummarize, cancellationToken);
            var pendingRequest = pendingRequests.Register(requestId.Value, isSummarization: true);
            var callbackResult = await pendingRequest.Completion.Task.WaitAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(callbackResult.ErrorMessage))
            {
                ConsoleHelper.WriteLineWithColor(callbackResult.ErrorMessage, ConsoleColor.Red);
                return;
            }

            var summary = callbackResult.SummarizationResult ?? throw new InvalidOperationException("The API returned no summarization result.");
            var preservedMessages = conversationState.Conversation.Skip(includeCount).ToList();
            conversationState.Conversation.Clear();
            conversationState.Conversation.Add(new ContextMessage
            {
                Role = ContextMessageRole.Assistant,
                Date = summary.SummarizedContentDatetime,
                Text = summary.SummarizedContent
            });
            conversationState.Conversation.AddRange(preservedMessages);
            conversationState.TokensCount = 100;
            ConsoleHelper.WriteLineWithColor("Chat conversation has been summarized.", ConsoleColor.Green);
        }
        catch (OperationCanceledException)
        {
            if (requestId.HasValue)
            {
                pendingRequests.TryAbandon(requestId.Value);
            }

            ConsoleHelper.WriteLineWithColor("Summarization canceled.", ConsoleColor.Yellow);
        }
        catch (Exception exception)
        {
            ConsoleHelper.WriteLineWithColor(exception.Message, ConsoleColor.Red);
        }
    }

    private async Task PrintConfigurationsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var configuration = await apiClient.GetConfigurationSummaryAsync(cancellationToken);
            Console.WriteLine($"Sandbox:\n\tUrl: {configuration.SandboxServiceUrl}\n\tName: {configuration.SandboxName}\n\tAgentId: {configuration.AgentId}\n");
            Console.WriteLine("Agent configurations:");
            foreach (var agent in configuration.Agents)
            {
                ConsoleHelper.PrintAgentConfiguration(agent.AgentRole, agent.Model, agent.Temperature);
            }
            Console.WriteLine();
        }
        catch (Exception exception)
        {
            ConsoleHelper.WriteLineWithColor($"Unable to retrieve API configuration: {exception.Message}", ConsoleColor.Red);
        }
    }

    private void PrintConversationStatus()
    {
        ConsoleHelper.WriteLineWithColor($"\n\nConversation status:\nCount of messages {conversationState.Conversation.Count}\nCount of tokens: {conversationState.TokensCount}\nCumulated cost: {Math.Round(conversationState.CumulatedCost, 2)} $", ConsoleColor.Gray);
    }

    private static async Task MonitorRequestCancellationByKeyboardAsync(CancellationTokenSource requestCancellation, CancellationToken applicationCancellationToken)
    {
        while (!requestCancellation.IsCancellationRequested && !applicationCancellationToken.IsCancellationRequested)
        {
            if (Console.KeyAvailable)
            {
                var keyInfo = Console.ReadKey(intercept: true);
                if (keyInfo.Key == ConsoleKey.C && keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control))
                {
                    requestCancellation.Cancel();
                    ConsoleHelper.WriteLineWithColor("\nCurrent request cancellation requested...", ConsoleColor.Yellow);
                    return;
                }
            }

            try
            {
                await Task.Delay(50, applicationCancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Available commands:");
        Console.WriteLine("/help - Show this help message");
        Console.WriteLine("/exit - Exit the application");
        Console.WriteLine("/new - Initializes a new conversation");
        Console.WriteLine("/summarize - Summarizes the current conversation");
        Console.WriteLine("Ctrl+C - Cancel the current request");
        Console.WriteLine("Any other text will be treated as a question to the AgentMesh system.\n");
    }
}
