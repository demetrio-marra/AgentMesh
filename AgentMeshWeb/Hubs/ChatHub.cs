using AgentMeshWeb.Services;
using Microsoft.AspNetCore.SignalR;

namespace AgentMeshWeb.Hubs
{
    public sealed class ChatHub(ChatCoordinator coordinator) : Hub
    {
        public async Task Initialize(string chatId)
        {
            ValidateChatId(chatId);
            await Groups.AddToGroupAsync(Context.ConnectionId, ChatCoordinator.GroupName(chatId));
            await coordinator.InitializeAsync(chatId, Context.ConnectionAborted);
        }

        public Task Submit(string chatId, string message)
        {
            ValidateChatId(chatId);
            return coordinator.SubmitAsync(chatId, message, Context.ConnectionAborted);
        }

        public Task Stop(string chatId)
        {
            ValidateChatId(chatId);
            return coordinator.StopAsync(chatId, Context.ConnectionAborted);
        }

        public Task NewChat(string chatId)
        {
            ValidateChatId(chatId);
            return coordinator.NewChatAsync(chatId, Context.ConnectionAborted);
        }

        private static void ValidateChatId(string chatId)
        {
            if (!Guid.TryParse(chatId, out _))
            {
                throw new HubException("Invalid chat session.");
            }
        }
    }
}