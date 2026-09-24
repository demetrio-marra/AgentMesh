namespace AgentMesh.Models;

public sealed class ConversationState
{
    public List<ContextMessage> Conversation { get; } = [];
    public int TokensCount { get; set; }
    public decimal CumulatedCost { get; set; }

    public void Reset()
    {
        Conversation.Clear();
        TokensCount = 0;
        CumulatedCost = 0;
    }
}
