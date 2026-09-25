## Why

The README names AgentMesh's infrastructure adapters, but the "Why AgentMesh" section does not clearly tell developers which external services they can use when building custom pipelines. A concise connector list with recognizable service links and badges will make those extension options discoverable without requiring readers to inspect the architecture or source tree.

## What Changes

- Add a connector-focused bullet to the README's "Why AgentMesh" section.
- Identify the supported connector categories: OpenAI ChatCompletions-compatible endpoints, Mem0, LightRAG, reranker-compatible endpoints, and JSCodeSandbox.
- Link each named service to its relevant public site or repository and add badges where a stable, useful badge is feasible.
- Keep the wording aligned with the existing architecture description and make clear that developers can employ these connectors in custom pipelines.

## Non-goals

- Implementing or changing any connector, adapter, API, configuration, or dependency.
- Changing runtime behavior, pipeline contracts, or service support levels.
- Adding badges whose source is unavailable or misleading; the implementation may use linked text for those services instead.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `architecture-documentation`: Extend the architecture reference so the README explicitly presents the available external-service connectors as options for custom pipeline developers.

## Impact

The only implementation target is the repository root `README.md`, specifically its "Why AgentMesh" section. The change affects discoverability and documentation only; it has no runtime, API, package, or deployment impact.