## Context

The root README already describes AgentMesh's infrastructure adapters in the architecture table and lists their internal service boundaries later in the document. The requested improvement is a discoverability change in the "Why AgentMesh" section, constrained to documentation and informed by the existing adapter layering. The configured architecture-baseline reference is not present at its declared path, so this design relies on the current README and the existing `architecture-documentation` capability.

## Goals / Non-Goals

**Goals:**

- Add one concise connector-oriented entry to the existing benefits list.
- Cover the five requested connector categories with accurate labels and links.
- Use linked badges from recognizable, stable sources where they improve scanning without overstating support.

**Non-Goals:**

- Altering adapter code, service contracts, package references, configuration, or runtime behavior.
- Creating a new badge-generation system or adding external build dependencies.

## Decisions

- **Place the content in the existing "Why AgentMesh" list.** This is the requested first-section location and puts connector availability beside the framework's other developer-facing benefits. A new top-level section would add navigation overhead for a small addition.
- **Describe compatibility accurately.** OpenAI and reranker entries will describe compatible endpoints rather than implying a single vendor is required; Mem0, LightRAG, Cohere, and JSCodeSandbox will use the requested product or repository names and supplied links.
- **Use Markdown links and shields-style badges only where appropriate.** Badges will remain presentation aids, while the linked text remains the durable documentation contract. If a reliable badge target is unavailable, linked text is preferred over an invented or unstable badge.
- **Keep the change self-contained.** No README restructuring or changes to the already-correct architecture and external-boundary sections are needed.

## Risks / Trade-offs

- [External badge or service URLs change] -> Keep the service name and destination link readable in Markdown so the documentation remains understandable if a badge image later fails.
- [Badge branding implies stronger support than implemented] -> Use compatibility wording and retain the existing architecture's adapter terminology; do not claim certifications or first-party integrations.