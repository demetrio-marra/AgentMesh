## 1. Package Boundary Preparation

- [ ] 1.1 Identify the abstract agent, contracts, models, configuration loading, parameter store, plugin loader, and resilience types that remain framework APIs; expose/package required application-layer APIs and verify `AgentMesh.Framework` and `AgentMesh.Framework.Application` pack successfully.
- [ ] 1.2 Configure a NuGet package source and aligned package versions for local consumption, then replace CLI and API AgentMesh framework project references with package references and verify `dotnet restore AgentMesh.sln` completes without direct framework project references.
- [ ] 1.3 Move host-owned configuration and content-file handling out of the extracted pipeline graph, retain only host/infrastructure settings in the framework package, and verify CLI/API builds resolve their required runtime assets from packages.

## 2. Create the Default Pipeline Plugin

- [ ] 2.1 Add `samples/AgentMesh.DefaultPipelinePlugin` to the solution as a class library using NuGet package references only, and verify its project file contains no `ProjectReference` to `AgentMesh.Application`.
- [ ] 2.2 Move `ChatRequestPipeline`, `SummarizationPipeline`, and all pipeline-owned parameters into the sample plugin while preserving their parameter initialization and branch behavior; verify the solution builds.
- [ ] 2.3 Move all dependent EW steps and concrete agents, including `ConversationSummarizerAgent`, into the sample plugin while retaining only `AbstractAgent<T>` and other shared abstractions in the application framework package; verify the moved agents compile against packaged APIs.
- [ ] 2.4 Move the required prompt files, plugin-owned models, parameter serializers/helpers, utilities, configurations, and exceptions into the sample plugin; verify each configured agent prompt/resource resolves from the plugin deployment content.
- [ ] 2.5 Implement `IAgentMeshPluginBootstrap` to register both pipeline interfaces and all plugin-owned services with the required lifetimes; verify a service provider resolves `IChatRequestPipeline` named `default` and `ISummarizationPipeline` after the bootstrap runs.

## 3. Remove Built-In Pipeline Ownership

- [ ] 3.1 Remove the extracted concrete pipeline graph and direct `IChatRequestPipeline`/`ISummarizationPipeline` registrations from `AgentMesh.Application`, retaining host runtime services and framework-provided infrastructure implementations; verify no duplicate extracted types remain.
- [ ] 3.2 Update `AgentMeshRuntime`, CLI, and API composition/configuration code for package-only consumption and plugin-supplied pipelines; verify `dotnet build AgentMesh.sln` succeeds.
- [ ] 3.3 Ensure no project target, post-build action, or packaging rule copies the sample plugin into CLI or API `bin/Plugins`; verify build output configuration contains no host-plugin deployment action.
- [ ] 3.4 Replace `AppInstance` automatic threshold-triggered summarization with an explicit operation over its in-memory conversation, and verify a normal chat request never resolves or executes `ISummarizationPipeline`.
- [ ] 3.5 Add CLI `/summarize` command handling that invokes the explicit application operation and displays its outcome; verify `/help` documents the command and `/new` still resets the conversation.

## 4. Validate Runtime Behavior

- [ ] 4.1 Manually stage the sample plugin assembly and all of its runtime dependencies in the CLI plugin directory, start interactive mode, and verify the `default` pipeline is discovered, normal requests never summarize automatically at the threshold, and `/summarize` invokes plugin-provided summarization.
- [ ] 4.2 Manually stage the sample plugin assembly and all of its runtime dependencies in the API plugin directory, call the named/default request endpoint with caller-provided context, and verify the API remains stateless and does not invoke summarization.
- [ ] 4.3 Run `openspec validate "extract-chat-pipeline-plugin" --strict` and the relevant build/test suite, then record that package-only references, manual deployment, explicit CLI summarization, no automatic summarization, and API stateless execution all pass.