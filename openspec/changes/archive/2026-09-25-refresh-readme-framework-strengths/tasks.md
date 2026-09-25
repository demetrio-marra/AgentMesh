## 1. Reframe the README entry point

- [x] 1.1 Add a concise AgentMesh positioning statement and verified .NET 8, ASP.NET Core, Docker, and Kubernetes header badges to `README.md`; verify badge URLs render in a GitHub-compatible Markdown preview and do not imply unsupported CI, provider, or packaged features.
- [x] 1.2 Place the strengths-led "Why AgentMesh" section immediately after the existing introductory paragraph and before the architecture reference; verify it identifies the pipeline as the orchestration backbone and parameters as the step data-exchange boundary.

## 2. Document framework differentiators

- [x] 2.1 Describe declared step inputs/outputs and automatic before/after parameter-change reporting; verify the wording matches `IEWStep` and `EWPipeline` behavior.
- [x] 2.2 Describe configurable agent-input and parameter-display serialization, including truncation or omission use cases; verify the wording matches `IAgentInputSerializer` and `IEWParameterSerializer` without requiring JSON.
- [x] 2.3 Describe token usage and cost accounting, including per-million-token pricing and optional hourly pricing; verify no text claims a per-minute calculator.
- [x] 2.4 Describe the authenticated REST chat and summarization APIs, including context cleanup via a summarization pipeline; verify the described endpoints remain aligned with `RequestsController`.

## 3. Preserve operational guidance and validate documentation

- [x] 3.1 Retain and refine the existing plugin startup, packaging, Docker, Kubernetes, request-flow, and external-boundary sections; verify links to the external default-plugin README and existing command snippets remain valid.
- [x] 3.2 Qualify deployment guidance so Dockerfiles and Kubernetes manifests remain plugin-owned; verify the README does not claim checked-in Kubernetes manifests or runtime dynamic plugin loading.
- [x] 3.3 Review the completed README in rendered Markdown and against the referenced code surfaces; verify heading hierarchy, badge accessibility text, code fences, links, and all capability claims are accurate.