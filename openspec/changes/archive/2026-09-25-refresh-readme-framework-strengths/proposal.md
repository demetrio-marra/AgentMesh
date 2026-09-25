## Why

The README accurately describes AgentMesh internals but does not quickly communicate why the framework is useful to prospective plugin authors and operators. A strengths-led entry point will make its pipeline model, observability, extensibility, API, and deployment value discoverable without overstating current behavior.

## What Changes

- Keep the existing introductory paragraph directly below the title and header badges, then place a strengths-led pipeline-first overview immediately before the detailed architecture reference.
- Document parameter-store-mediated step data flow, automatically reported parameter mutations, declared step inputs and outputs, configurable agent input serialization, and parameter display serialization.
- Describe built-in token usage and pricing accounting accurately, including token pricing and optional time-based cost configuration.
- Surface the authenticated REST request and summarization endpoints, plugin packaging, Docker deployment, and Kubernetes deployment guidance.
- Add a concise header badge row using verified project technologies, including .NET 8, ASP.NET Core, Docker, Kubernetes, and package distribution where applicable.
- Preserve existing setup, packaging, deployment, request-flow, external-boundary, and licensing information while improving scanability and cross-links to the maintained default plugin.

## Capabilities

### New Capabilities

None. This change is documentation-only and does not introduce a product capability.

### Modified Capabilities

None. The existing architecture documentation requirements remain unchanged; this refresh improves presentation of the root README.

## Impact

- Affected documentation: root `README.md`.
- No runtime code, HTTP contract, dependency, configuration, or deployment-manifest changes.
- README claims must be supported by the current framework and distinguish framework capabilities from plugin-owned operational assets.

## Non-goals

- Adding Kubernetes manifests, CI badges, new deployment automation, or a new sample plugin.
- Changing token/cost accounting behavior, pipeline contracts, serialization APIs, or REST endpoints.
- Claiming that AgentMesh calculates a per-minute charge; the current model supports token-based pricing and optional time-based cost configuration.