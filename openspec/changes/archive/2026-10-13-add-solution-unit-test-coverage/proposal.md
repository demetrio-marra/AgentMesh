## Why

AgentMesh currently has one xUnit project covering only the Responses adapter, leaving the core, application, runtime, other infrastructure adapters, and host projects without repeatable isolated verification. A solution-wide unit-test baseline is needed before changes can be made with confidence.

## What Changes

- Add xUnit unit-test projects for every production project in `AgentMesh.sln`, following the existing Responses test-project pattern.
- Add focused tests for public behavior, branch decisions, validation, error handling, and data mapping in each layer, using fakes or mocks at external boundaries.
- Add solution-level coverage collection and reporting so the test suite provides a measurable coverage baseline.
- Keep the existing Responses adapter tests and extend them only where meaningful isolated branches remain untested.

## Capabilities

### New Capabilities

None. This change adds verification tooling and tests without changing runtime behavior.

### Modified Capabilities

None.

## Non-goals

- Integration, end-to-end, browser, network, Docker, or live-provider tests.
- Production behavior, public API, or configuration changes except test-only project and coverage configuration.
- A mandated numeric coverage threshold; the initial report establishes the baseline and highlights untestable external-host code.

## Impact

`AgentMesh.sln`, a new or expanded `tests/` project for each production project, test-only package references and project references, and coverage/report configuration. External services will be substituted through existing interfaces or focused adapters; composition roots will be tested only for deterministic local behavior.