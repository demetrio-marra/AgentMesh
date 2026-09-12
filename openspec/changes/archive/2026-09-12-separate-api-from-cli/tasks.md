## 1. Extract Shared Host Composition

- [x] 1.1 Identify the common configuration, infrastructure registration, plugin bootstrap, pipeline registration, and initialization code in `AgentMeshCLI`, then expose it through a reusable host-composition surface; verify the resulting API is free of console and web-only registrations by building the affected project.
- [x] 1.2 Update the CLI startup to call the shared composition, register `ConsoleWorkflowProgressNotifier` and `UserConsoleInputService`, and run the console host unconditionally; verify `AgentMeshCLI` contains no `--interactive` mode branch and builds successfully.

## 2. Create the API Host

- [x] 2.1 Create the `AgentMesh.Api` ASP.NET Core executable with references to the shared host composition and required application/infrastructure projects; verify the project is present in the solution and restores successfully.
- [x] 2.2 Move the request controller, API-key authentication types/configuration, and API-specific models into `AgentMesh.Api` while preserving namespaces, routes, authentication scheme behavior, and response contracts; verify the API project compiles.
- [x] 2.3 Implement API startup with configuration loading, shared service registration, `DummyWorkflowProgressNotifier`, `StatelessAppInstance`, authentication/authorization, controllers, Swagger, and XML documentation; verify the web host starts and exposes Swagger.
- [x] 2.4 Provide API runtime configuration and content-copy rules for appsettings, prompts, and other required deployment files without removing the CLI's console configuration; verify both project outputs contain the files needed by their selected host.

## 3. Update Solution and Launching

- [x] 3.1 Add `AgentMesh.Api` to `AgentMesh.sln` and update project references/build configuration; verify `dotnet build AgentMesh.sln` succeeds.
- [x] 3.2 Replace the launch profiles with `Interactive` targeting the console project and `Web` targeting `AgentMesh.Api`, including the existing development environment and Swagger launch URL for Web; verify each profile selects the intended executable.
- [x] 3.3 Update the active debug profile and any related project metadata/documentation to use the new profile names; verify no launch configuration refers to the removed mode-selection behavior.

## 4. Validate Runtime Compatibility

- [x] 4.1 Start the `Interactive` profile and verify it enters the existing stateful console workflow without requiring `--interactive`.
- [x] 4.2 Start the `Web` profile and verify the API responds at the existing routes, Swagger loads, and a valid API-key-authenticated request preserves default and named pipeline routing behavior.
- [x] 4.3 Run the full solution build and inspect source references for API controllers, authentication, Swagger, and `--interactive` to verify API-only concerns are absent from the console project.