## 1. Application Summary Contract

- [x] 1.1 Add the application-layer configuration summary model containing the sandbox, user agent, and per-agent summary fields; verify it remains independent of `AgentMesh.Api` types.
- [x] 1.2 Add an `AppInstance` operation that receives the existing configuration dependencies and performs agent enumeration plus invariant-culture temperature conversion; verify the returned values match the current controller mapping.

## 2. API Delegation

- [x] 2.1 Update `ConfigurationController` to depend on `AppInstance`, remove its direct configuration dependencies, and project only the application result into the existing API DTO; verify the route, authorization attributes, and response shape remain unchanged.
- [x] 2.2 Build `AgentMesh.Application` and `AgentMesh.Api` to verify dependency direction and compilation, without adding or modifying tests.