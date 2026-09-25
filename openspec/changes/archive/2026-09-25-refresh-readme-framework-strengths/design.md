## Context

See `proposal.md` for motivation. The current root README is a technically accurate architecture and operations reference, but its opening does not prioritize the framework's pipeline-oriented value. The implementation is confined to `README.md`; the current application and runtime code provide the factual basis for every new claim.

`EWPipeline` snapshots declared `IEWStep` inputs and outputs, commits mutations through the parameter store, and exposes before/after display records. `IAgentInputSerializer` and `IEWParameterSerializer` are explicit extension points. `AgentExecutionCost` supports either token-based pricing or optional hourly pricing. The Runtime request controller exposes chat and summarization endpoints. The reference plugin has a .NET 8 Dockerfile, while Kubernetes deployment guidance is documentation rather than a checked-in manifest.

## Goals / Non-Goals

**Goals:**
- Make AgentMesh's differentiated model understandable from the opening README sections.
- Preserve the existing README as the durable setup, packaging, deployment, and API reference.
- Ensure badges and prose reflect repository-verifiable technologies and behavior.

**Non-Goals:**
- Create a visual rebrand, marketing site, Kubernetes manifests, or CI workflow.
- Duplicate the complete architecture reference or add a new documentation subsystem.

## Decisions

### Decision 1: Lead with an outcome-oriented framework overview
- **Choice:** Preserve the existing introductory paragraph below the title and badge row, then place the "Why AgentMesh" strengths section immediately after it and ahead of the detailed architecture table.
- **Rationale:** Readers can understand the pipeline-first abstraction and supported operational model before navigating implementation detail.
- **Alternatives considered:**
  - Retain the architecture table as the opening: accurate but low-context for new readers.
  - Replace the README with a marketing page: would obscure operational guidance that users already rely on.

### Decision 2: Document strengths as evidence-backed capability cards or concise subsections
- **Choice:** Group the provided strengths into pipeline orchestration, observability and presentation, extensibility, cost awareness, API and context management, and deployment.
- **Rationale:** This avoids a flat feature list and lets related capabilities explain one another while preserving precision.
- **Alternatives considered:**
  - List every interface and implementation class: too brittle and too technical for the entry point.
  - Describe agents as the framework backbone: conflicts with the actual pipeline and parameter-store control flow.

### Decision 3: Qualify operational claims in the README
- **Choice:** State that token usage is tracked and priced from configured per-million-token rates, and that optional time-based pricing uses hourly configuration; state that plugin applications own their Dockerfiles and Kubernetes manifests while AgentMesh supplies deployment guidance/templates.
- **Rationale:** These descriptions match `AgentExecutionCost`, the runtime, and the reference plugin without promising a per-minute calculator or bundled Kubernetes assets.
- **Alternatives considered:**
  - Repeat the initial request's per-minute phrasing: inaccurate for the current model.
  - Omit the deployment qualifier: could imply framework-provided Kubernetes manifests.

### Decision 4: Use only repository-verifiable header badges
- **Choice:** Add compact Shields.io badges for .NET 8, ASP.NET Core, Docker, and Kubernetes; add a NuGet/package badge only after confirming an applicable public package endpoint and name.
- **Rationale:** These technologies are evidenced by project targets, runtime controllers, the reference Dockerfile, and existing Kubernetes guidance. Static badges remain stable without adding build infrastructure.
- **Alternatives considered:**
  - Add CI, coverage, provider, or version badges: no matching repository evidence or verified public endpoint.
  - Use bespoke image assets: unnecessary maintenance for standard technology identifiers.

## Risks / Trade-offs

- **[Risk]** Documentation can drift from implementation. → Mitigation: tie each strengths claim to an existing public abstraction or execution behavior and retain concise wording.
- **[Risk]** Remote badge providers can fail to render. → Mitigation: use standard static badge URLs with descriptive alt text; README remains usable without them.
- **[Risk]** The larger README becomes harder to scan. → Mitigation: retain current headings where practical, add a focused contents structure only when it improves navigation, and avoid duplicating detailed material.

## Migration Plan

1. Edit only `README.md`, preserving all still-valid setup and operational sections.
2. Render the Markdown in GitHub-compatible preview and check external links and badge URLs.
3. Compare every new claim against its cited framework surface and verify existing command snippets remain intact.

Rollback is a single-file documentation revert; no release, configuration migration, or runtime rollback is required.