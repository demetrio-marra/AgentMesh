## 1. Verify Current Architecture Evidence

- [x] 1.1 Review the project files, package metadata, API and CLI startup code, and sample plugin to confirm the six project-role statements; verify every documented relationship against a concrete repository source.
- [x] 1.2 Confirm no runtime or public API behavior is included in the documentation scope; verify the planned diff is limited to `README.md` and existing OpenSpec main specifications.

## 2. Refresh Canonical Architecture Documentation

- [x] 2.1 Update the README architecture overview and project-structure table with the complete project-role model; verify all six project names and responsibilities appear once and agree with project metadata.
- [x] 2.2 Update README packaging, plugin-hosting, runtime-flow, and setup prose to identify `AgentMesh.Application` as the plugin-facing framework, `AgentMesh.Api` as the custom-pipeline host, and `AgentMeshCLI` as its REST terminal frontend; verify no section assigns pipeline-host responsibility to the CLI.
- [x] 2.3 Update the main `architecture-documentation` specification with the authoritative layer, dependency, reusable-package, and executable-host requirements; verify every requirement retains at least one valid scenario.

## 3. Align Existing Capability Specifications

- [x] 3.1 Update `api-cli-separation`, `api-configuration-summary`, `api-summarization-pipeline`, `async-request-callbacks`, and `cli-api-frontend` with consistent API-host and REST-terminal ownership wording; verify their existing behavior requirements remain unchanged.
- [x] 3.2 Update `default-chat-pipeline-plugin`, `plugin-pipeline-hosting`, and `stateless-pipeline-runner` with the plugin-facing framework, host, domain, contract, and adapter responsibilities; verify plugin dependencies continue to target reusable framework packages rather than the host.
- [x] 3.3 Update `request-access-modes` with the API-host versus application-framework execution distinction; verify its authentication and request-routing behavior remains unchanged.

## 4. Review Documentation Consistency

- [x] 4.1 Search the README and all main OpenSpec specs for `AgentMesh`, `AgentMesh.Contracts`, `AgentMesh.Infrastructure`, `AgentMesh.Application`, `AgentMesh.Api`, and `AgentMeshCLI`; verify every relevant occurrence uses the agreed responsibility terminology.
- [x] 4.2 Review the complete documentation diff for stale statements that present the CLI as a pipeline host or the API as the reusable plugin framework; verify no code, project, package, or configuration files were changed.

## 5. Validate Change Artifacts

- [x] 5.1 Run `openspec validate refresh-architecture-documentation --strict`; verify the change remains valid after implementation.
- [x] 5.2 Inspect the final README and OpenSpec specification changes against the delta requirements; verify each scenario can be satisfied from the delivered documentation.