namespace AgentMesh.Models.Rerank
{
    public readonly record struct RerankInputQuery
    {
        public required string Query { get; init; }
        public required List<string> CandidateDocuments { get; init; }
        public int? TopN { get; init; }
    }
}
