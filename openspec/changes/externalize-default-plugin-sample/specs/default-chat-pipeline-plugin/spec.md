## MODIFIED Requirements

### Requirement: Complete default pipeline reference plugin SHALL be documented externally
AgentMesh documentation SHALL identify `https://github.com/demetrio-marra/AMCodePipeline` as the externally maintained executable reference plugin for the default chat and conversation-summarization pipelines. AgentMesh SHALL NOT distribute that reference plugin's parameters, steps, concrete agents, prompt content, models, helpers, utilities, configuration, exceptions, composition root, or deployment assets as an in-repository sample project.

#### Scenario: Developer locates the default pipeline reference
- **WHEN** a developer reads the AgentMesh plugin documentation
- **THEN** they are directed to the `AMCodePipeline` repository for the runnable reference plugin and its setup instructions

#### Scenario: Default pipeline is registered from the reference plugin
- **WHEN** the documented reference plugin starts and identifies its assembly to Runtime
- **THEN** Runtime reflection-registers the plugin's components and exposes its single chat pipeline

#### Scenario: Repository no longer contains the bundled sample
- **WHEN** a developer follows the AgentMesh README or repository paths
- **THEN** no command or link targets `samples/AgentMesh.DefaultPipelinePlugin` or represents it as a bundled executable sample

#### Scenario: Default pipeline request behavior is retained
- **WHEN** a request reaches the documented reference plugin service
- **THEN** it follows the same request-analysis, knowledge, task-execution, and response-composition branches as the prior default pipeline

#### Scenario: CLI user explicitly summarizes conversation context
- **WHEN** an interactive CLI user enters `/summarize` against the documented reference plugin
- **THEN** the plugin service invokes its summarization pipeline for the supplied conversation and returns the summary used by the CLI

#### Scenario: CLI starts without a summarization pipeline
- **WHEN** the CLI frontend targets a plugin service that does not expose the standard summarization behavior
- **THEN** summarization availability fails with an error identifying the required plugin-provided summarization pipeline

#### Scenario: Reaching the threshold does not summarize automatically
- **WHEN** an interactive-mode conversation context reaches or exceeds the configured summarization threshold during a normal chat request
- **THEN** the CLI retains the context without invoking the summarization endpoint until the user enters `/summarize`

#### Scenario: API mode keeps caller-owned context
- **WHEN** an API client sends a request with optional conversation messages
- **THEN** the plugin service passes that context to its chat pipeline without retaining server-side context or invoking the summarization pipeline

### Requirement: Plugin documentation SHALL identify the framework package
Documentation for the default chat pipeline plugin SHALL identify `AgentMesh.Runtime` as the plugin-facing package, identify `AMCodePipeline` as the external reference project, and state that a plugin project owns configuration, composition, HTTP execution, and deployment. It SHALL direct developers to the external project's repository for its current run and Docker instructions.

#### Scenario: Plugin author selects dependencies
- **WHEN** a plugin author reads the default pipeline plugin documentation
- **THEN** they can identify the Runtime package, the plugin-owned files required to run and deploy a pipeline service, and the external repository that demonstrates them