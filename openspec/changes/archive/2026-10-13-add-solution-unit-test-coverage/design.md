## Context

See `proposal.md` for motivation. `AgentMesh.sln` contains 13 production projects and only `AgentMesh.Infrastructure.Responses.Tests` today. That project establishes the local convention: .NET 8 xUnit tests with a project reference, constructor injection of a fake boundary, and no network calls. The architecture is layered: core and contracts underpin runtime and application orchestration; infrastructure projects adapt external APIs; CLI, web, and callback viewer are host surfaces.

## Goals / Non-Goals

**Goals:**
- Give every production project a discoverable xUnit test project included in the solution.
- Cover deterministic public behavior and meaningful failure paths while replacing every external dependency with a fake, mock, stub handler, or in-memory configuration.
- Produce a solution-level coverage report that includes production assemblies and excludes test and generated code.
- Keep test fixtures small and layer-aligned so tests remain fast and independent.

**Non-Goals:**
- Test provider connectivity, HTTP transport, JavaScript sandbox execution, hosted service lifecycles, rendered UI, or callback delivery against real processes.
- Change production visibility solely to test private implementation details; tests must exercise public behavior or existing seams.
- Add a CI threshold before the initial measured baseline is reviewed.

## Decisions

### Create one xUnit project per production assembly

Each `src/<Project>` assembly will receive a matching `tests/<Project>.Tests` project using the shared .NET 8 default, `Microsoft.NET.Test.Sdk`, xUnit, and the Visual Studio runner. A test project targets a newer framework only when required to reference its production assembly; `CallbackViewer.Tests` targets .NET 9 to match `CallbackViewer`. Projects will be added to `AgentMesh.sln` and reference only the assembly under test plus test-only dependencies needed to replace its boundaries.

This gives ownership and failure reporting that map directly to solution projects. A single shared test project was rejected because it would blur ownership and accumulate unnecessary transitive dependencies.

### Test behaviors at architectural seams

Core and contracts tests will verify core model calculations, records, validation, DTO defaults, and serialization behavior. Runtime tests will cover deterministic runtime-owned API, authentication, configuration, and helper behavior using in-memory dependencies. Application tests will cover parameter-store snapshots and versioned commits, pipeline state transitions, routing, branching, retry/error mapping, workflow-state mutation, and agent response parsing through fake contracts. Adapter tests will use in-memory HTTP handlers, fake SDK clients, or faked process abstractions to assert request construction and response/error mapping. Host-project tests will remain limited to deterministic configuration, formatting, component/view-model logic, and registration helpers that do not start a host.

Testing private members or executing real external dependencies was rejected because it couples the suite to implementation details and violates the unit-only boundary.

### Reuse small local test doubles and shared utilities only when truly common

Each project will own fakes that model its contract behavior. A minimal shared test-support project may be added only for generic helpers (for example, an in-memory configuration builder or recording logger); it must not reference production assemblies or contain project-specific fixtures.

This preserves locality while avoiding repeated boilerplate. A broad mock framework or an all-purpose fixture library is not required initially; choose the smallest established dependency when a project needs interaction verification.

### Collect coverage as an observable baseline

The solution test invocation will collect Cobertura coverage for production assemblies, excluding test assemblies and generated/compiler artifacts, and write reports to ignored test-result directories. Initial implementation will publish the aggregate and per-assembly results without a numeric gate; a later change may establish thresholds from the measured baseline.

This makes coverage progress measurable without rewarding superficial tests. A hard threshold now was rejected because no meaningful baseline exists.

## Risks / Trade-offs

- [Some host or external-wrapper code has no injectable seam] -> Cover deterministic public behavior only; record justified exclusions rather than adding network tests or changing production behavior.
- [Project-per-project fixtures can repeat setup] -> Extract only generic, proven repetition into test support after two or more consumers emerge.
- [Coverage can favor trivial code] -> Prioritize decision branches, error handling, and state transitions; review uncovered external-host areas alongside the report.
- [A 13-project suite may lengthen feedback] -> Keep all doubles in-process and tests parallel-safe; run individual projects during development and the solution suite for validation.

## Migration Plan

1. Add shared test/coverage conventions and test projects to the solution.
2. Add focused tests in dependency order: core/contracts, runtime and application, infrastructure, then host projects.
3. Run each test project during implementation, then collect solution-wide coverage and review exclusions.
4. Roll back by removing test-only projects, solution entries, and coverage configuration; production runtime behavior and deployed services are unaffected.