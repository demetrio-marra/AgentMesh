using System.ComponentModel.DataAnnotations;

namespace AgentMeshWeb.Configuration
{
    public sealed class ApiConfiguration
    {
        public const string SectionName = "Api";

        [Required, Url]
        public string BaseUrl { get; set; } = "http://localhost:5000";

        [Required]
        public string ApiKey { get; set; } = string.Empty;

        [Required]
        public string HeaderName { get; set; } = "X-Api-Key";
    }
}