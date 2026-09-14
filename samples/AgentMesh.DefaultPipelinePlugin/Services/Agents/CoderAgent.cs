using AgentMesh.Application.Contracts;
namespace AgentMesh.Application.Services.Agents
{
    public sealed partial class CoderAgent(IOpenAIClientFactory openAIClientFactory,
                      Resilience resilience,
                      ILogger<CoderAgent> logger,
                      IAgentInputSerializer agentInputSerializer) : AbstractAgent<string>(logger,
                          "Coder",
                          openAIClientFactory,
                          resilience,
                          agentInputSerializer)
    {
        private readonly Regex JavascriptCodeRegex = JavascriptCodeRegexCompiled();

        private readonly ILogger<CoderAgent> _logger = logger;

        protected override IEnumerable<AgentInputParameterConfiguration> GetAgentInputParameterConfiguration()
        {
            return [
                new()
                {
                    ParameterType = typeof(RequestDateTimeParameter),
                    ParameterTags = [ParameterTags.AgentSystemParameterTag]
                },
                new()
                {
                    ParameterType = typeof(KnowledgeContentForCoderParameter),
                    ParameterTags = [ParameterTags.AgentSystemParameterTag]
                }
            ];
        }

        protected override string ParseStructuredResponse(string rawResponseText)
        {
            var codeRegexMatch = JavascriptCodeRegex.Match(rawResponseText);
            if (!codeRegexMatch.Success)
            {
                throw new BadStructuredResponseException(rawResponseText, "The model's response did not contain any valid JavaScript code block.");
            }

            return codeRegexMatch.Groups["code"].Value.Trim();
        }

        [GeneratedRegex(@"```\s*javascript\s*(?<code>(?:(?!```)[\s\S])*)\s*", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled, "it-IT")]
        private static partial Regex JavascriptCodeRegexCompiled();
    }
}
