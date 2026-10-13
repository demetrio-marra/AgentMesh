namespace AgentMesh.Contracts.Exceptions
{
    public class EmptyAgentResponseException : BadAgentResponseException
    {
        public EmptyAgentResponseException() : base("Empty agent response.")
        {
        }
    }
}