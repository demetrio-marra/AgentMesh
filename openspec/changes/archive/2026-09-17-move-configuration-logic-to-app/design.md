## Context

The existing configuration endpoint is an API adapter over configuration objects already available to `AppInstance`. `ConfigurationController` currently performs agent enumeration, temperature parsing, and response assembly. The API response DTOs are owned by `AgentMesh.Api`, while `AppInstance` is owned by `AgentMesh.Application`; the application project must not reference API presentation models. The configured architecture baseline file is absent from this checkout, so this design follows the repository's current project references and existing API configuration-summary contract.

## Goals / Non-Goals

**Goals:**

- Make `ConfigurationController` delegate configuration-summary computation to `AppInstance`.
- Keep application logic and configuration transformations in `AgentMesh.Application`.
- Preserve the endpoint's current response, authentication, field filtering, and error behavior.

**Non-Goals:**

- Changing API DTOs, routes, authentication, configuration sources, or startup behavior.
- Making `AppInstance` depend on API-only models.
- Adding or changing tests.

## Decisions

- Add an application-layer configuration summary model under `AgentMesh.Application.Models` and an `AppInstance` operation that returns it. This keeps the business result independent of HTTP serialization and avoids a project-reference cycle.
- Move agent enumeration, invariant temperature conversion, and summary value selection into `AppInstance`. The controller will retain only the HTTP-facing projection from the application model to `ConfigurationSummaryApiOutput`, which is presentation mapping rather than business logic.
- Inject `AppInstance` into `ConfigurationController` and remove its direct configuration dependencies. This makes the controller's dependency point explicit and preserves the existing singleton/scoped resolution model used by the API.
- Keep the existing API DTOs unchanged instead of relocating them to a shared project. Relocation would broaden the change and couple the core application contract to an HTTP response shape.

## Risks / Trade-offs

- [Risk] A new application model adds a small amount of type surface. -> Mitigation: keep it focused on the existing summary fields and use it only for this application operation.
- [Risk] Temperature parsing behavior could accidentally change during extraction. -> Mitigation: preserve invariant-culture conversion and the current source fields exactly.
- [Risk] The absent architecture baseline may contain additional guidance. -> Mitigation: validate the implementation against current project references and the existing `api-configuration-summary` and `api-cli-separation` specs.

## Migration Plan

Implement the application model and `AppInstance` operation, then update the controller constructor and `Get` method to delegate and project the result. Build the affected projects to verify dependency direction and compilation. Rollback is a source-only revert of the new model, application operation, and controller delegation; no data or deployment migration is required.