namespace AgentMesh.Models
{
    /// <summary>
    /// Represents a single message in a conversation history turn.
    /// </summary>
    public class ContextMessage
    {
        /// <summary>
        /// Role of the message author ("User" or "Assistant").
        /// </summary>
        public ContextMessageRole Role { get; set; }

        /// <summary>
        /// Date and time in UTC when the message was recorded.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Text content of the conversation message.
        /// </summary>
        public string Text { get; set; } = string.Empty;
    }
}
