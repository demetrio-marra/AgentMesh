using AgentMeshWeb.Configuration;
using AgentMeshWeb.Hubs;
using AgentMeshWeb.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace AgentMeshWeb.Services
{
    public sealed class ChatCoordinator(
        IAgentMeshApiClient apiClient,
        IChatContextStore contextStore,
        ChatOperationRegistry operationRegistry,
        IHubContext<ChatHub> hubContext,
        IOptions<ConversationSummarizationConfiguration> summarizationConfiguration)
    {
        private readonly ConversationSummarizationConfiguration _summarization = summarizationConfiguration.Value;

        public async Task InitializeAsync(string chatId, CancellationToken cancellationToken) =>
            await SendStateAsync(chatId, await contextStore.LoadAsync(chatId, cancellationToken), cancellationToken);

        public Task SubmitAsync(string chatId, string message, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new HubException("Enter a message before sending.");
            }

            ActiveChatOperation operation;
            try
            {
                operation = operationRegistry.Start(chatId);
            }
            catch (InvalidOperationException exception)
            {
                throw new HubException(exception.Message);
            }

            _ = RunChatAsync(operation, message.Trim());
            return Task.CompletedTask;
        }

        public async Task StopAsync(string chatId, CancellationToken cancellationToken)
        {
            operationRegistry.Stop(chatId);
            await SendStateAsync(chatId, await contextStore.LoadAsync(chatId, cancellationToken), cancellationToken);
            await SendOperationAsync(chatId, "canceled", cancellationToken);
        }

        public async Task NewChatAsync(string chatId, CancellationToken cancellationToken)
        {
            operationRegistry.Reset(chatId);
            await SendStateAsync(chatId, await contextStore.ClearAsync(chatId, cancellationToken), cancellationToken);
            await SendOperationAsync(chatId, "idle", cancellationToken);
        }

        private async Task RunChatAsync(ActiveChatOperation operation, string message)
        {
            try
            {
                var context = await contextStore.LoadAsync(operation.ChatId, operation.CancellationToken);
                await SendOperationAsync(operation.ChatId, "chat", operation.CancellationToken);
                var result = await apiClient.StreamChatAsync(
                    message,
                    context.Messages,
                    progress => SendProgressAsync(operation, progress),
                    operation.CancellationToken);

                if (!operationRegistry.IsCurrent(operation))
                {
                    return;
                }

                await hubContext.Clients.Group(GroupName(operation.ChatId)).SendAsync("WorkflowSummary", result, operation.CancellationToken);

                var completed = new ChatContextSnapshot(
                    context.Revision,
                    [.. context.Messages, new ContextMessage(ChatMessageRole.User, DateTime.UtcNow, message), new ContextMessage(ChatMessageRole.Assistant, DateTime.UtcNow, result.Message)],
                    result.CountOfTokens,
                    context.CumulatedCost + result.CumulatedCost);
                var saved = await contextStore.SaveAsync(operation.ChatId, context.Revision, completed, operation.CancellationToken);
                if (saved is null)
                {
                    await ReloadStateAsync(operation);
                    return;
                }

                await SendStateAsync(operation.ChatId, saved, operation.CancellationToken);
                if (ShouldSummarize(saved))
                {
                    await RunSummarizationAsync(operation, saved);
                }
            }
            catch (OperationCanceledException) when (operation.CancellationToken.IsCancellationRequested)
            {
                if (operationRegistry.IsCurrent(operation))
                {
                    await ReloadStateAsync(operation);
                    await SendOperationAsync(operation.ChatId, "canceled", CancellationToken.None);
                }
            }
            catch (Exception exception)
            {
                if (operationRegistry.IsCurrent(operation))
                {
                    await ReloadStateAsync(operation);
                    await hubContext.Clients.Group(GroupName(operation.ChatId)).SendAsync("Error", exception.Message);
                    await SendOperationAsync(operation.ChatId, "failed", CancellationToken.None);
                }
            }
            finally
            {
                var wasCurrent = operationRegistry.IsCurrent(operation);
                operationRegistry.Complete(operation);
                if (wasCurrent)
                {
                    await SendOperationAsync(operation.ChatId, "idle", CancellationToken.None);
                }
            }
        }

        private async Task RunSummarizationAsync(ActiveChatOperation operation, ChatContextSnapshot completed)
        {
            var preserveCount = Math.Min(completed.Messages.Count, _summarization.NumMessageToPreseve);
            var summarizedMessages = completed.Messages.Take(completed.Messages.Count - preserveCount).ToArray();
            if (summarizedMessages.Length == 0 || !operationRegistry.IsCurrent(operation))
            {
                return;
            }

            await SendOperationAsync(operation.ChatId, "summarizing", operation.CancellationToken);
            var summary = await apiClient.StreamSummarizationAsync(
                _summarization.SummarizeLanguage,
                summarizedMessages,
                progress => SendProgressAsync(operation, progress),
                operation.CancellationToken);
            if (!operationRegistry.IsCurrent(operation))
            {
                return;
            }

            var replacement = completed with
            {
                Messages = [new ContextMessage(ChatMessageRole.Assistant, summary.CreatedAt, summary.Content), .. completed.Messages.Skip(summarizedMessages.Length)],
                TokenCount = 100
            };
            var saved = await contextStore.SaveAsync(operation.ChatId, completed.Revision, replacement, operation.CancellationToken);
            if (saved is null)
            {
                await ReloadStateAsync(operation);
                return;
            }

            await SendStateAsync(operation.ChatId, saved, operation.CancellationToken);
        }

        private bool ShouldSummarize(ChatContextSnapshot context) =>
            context.TokenCount > _summarization.SummaryTokenThreshold &&
            context.Messages.Count > _summarization.NumMessageToPreseve;

        private async Task SendProgressAsync(ActiveChatOperation operation, WorkflowProgress progress)
        {
            if (operationRegistry.IsCurrent(operation))
            {
                await hubContext.Clients.Group(GroupName(operation.ChatId)).SendAsync("Progress", progress, operation.CancellationToken);
            }
        }

        private Task ReloadStateAsync(ActiveChatOperation operation) =>
            InitializeAsync(operation.ChatId, CancellationToken.None);

        private Task SendStateAsync(string chatId, ChatContextSnapshot snapshot, CancellationToken cancellationToken) =>
            hubContext.Clients.Group(GroupName(chatId)).SendAsync("State", new ChatState(snapshot.Messages, snapshot.TokenCount, snapshot.CumulatedCost, snapshot.Revision), cancellationToken);

        private Task SendOperationAsync(string chatId, string state, CancellationToken cancellationToken) =>
            hubContext.Clients.Group(GroupName(chatId)).SendAsync("Operation", state, cancellationToken);

        public static string GroupName(string chatId) => $"chat:{chatId}";
    }
}