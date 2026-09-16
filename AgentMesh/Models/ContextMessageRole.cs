using System.Text.Json.Serialization;

namespace AgentMesh.Models
{
    /// <summary>
    /// Specifies the author role of a message in a conversation.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ContextMessageRole
    {
        /// <summary>
        /// Message authored by the user / client.
        /// </summary>
        User,

        /// <summary>
        /// Message authored by the assistant / AI model.
        /// </summary>
        Assistant
    }
}
