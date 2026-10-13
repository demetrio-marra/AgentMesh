using System.ComponentModel.DataAnnotations;

namespace AgentMeshWeb.Configuration
{
    public sealed class ChatContextConfiguration
    {
        public const string SectionName = "ChatContext";

        [Range(1, 1440)]
        public int IdleExpirationMinutes { get; set; } = 120;
    }
}