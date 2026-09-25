# default-chat-pipeline-plugin Specification

## Purpose

Document an independently consumable external reference plugin for the default chat and summarization pipelines for AgentMesh plugin authors.

## Requirements

### Requirement: Complete default pipeline reference plugin SHALL be documented externally
AgentMesh documentation SHALL identify `https://github.com/demetrio-marra/AMCodePipeline` as the externally maintained executable reference plugin for the default chat and conversation-summarization pipelines. AgentMesh SHALL not distribute the reference plugin's implementation or deployment assets as an in-repository sample project.

#### Scenario: Default pipeline is registered from the reference plugin
- **WHEN** the reference plugin starts and identifies its assembly to Runtime
- **THEN** Runtime reflection-registers the plugin's components and exposes its single chat pipeline

#### Scenario: Default pipeline request behavior is retained
- **WHEN** a request reaches the reference plugin service
- **THEN** it follows the same request-analysis, knowledge, task-execution, and response-composition branches as the prior default pipeline

#### Scenario: CLI user explicitly summarizes conversation context
- **WHEN** an interactive CLI user enters `/summarize`
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

### Requirement: The sample plugin SHALL rely on host-provided framework services
The reference plugin SHALL consume application, agent memory, knowledge-store, reranker, code-sandbox, HTTP API, authentication, callback, and Swagger capabilities through the Runtime package. It SHALL not include replacement implementations for those framework or infrastructure services.

#### Scenario: Host provides infrastructure dependencies
- **WHEN** the sample plugin invokes Runtime initialization with valid infrastructure configuration
- **THEN** the plugin resolves its required framework service contracts and processes requests without embedding infrastructure adapters

#### Scenario: Runtime provides framework dependencies
- **WHEN** the sample plugin invokes Runtime initialization with valid configuration
- **THEN** its reflected components resolve their required framework services and process requests without embedding Runtime implementations

### Requirement: The sample plugin SHALL compile against published framework packages
The reference plugin project SHALL reference required AgentMesh framework APIs through the `AgentMesh.Runtime` NuGet package and SHALL not directly reference the Runtime or Application projects or the retired Application package.

#### Scenario: Plugin project has no direct application-project dependency
- **WHEN** a developer inspects the sample plugin project references
- **THEN** no project reference targets `AgentMesh.Application` and no package reference targets `AgentMesh.Framework.Application`

#### Scenario: Plugin project has no direct framework-project dependency
- **WHEN** a developer inspects the sample plugin project references
- **THEN** AgentMesh framework dependencies are NuGet package references and no project reference targets Runtime or Application

#### Scenario: Plugin receives Application transitively
- **WHEN** the sample plugin restores `AgentMesh.Runtime`
- **THEN** the package supplies the Application assembly required by the sample's framework-derived types

### Requirement: Concrete summarization configuration SHALL be CLI-owned
The concrete configuration class that defines the summarization language and retention policy SHALL be owned by the CLI project. Framework and plugin projects SHALL not define that concrete configuration class; the plugin SHALL consume only framework configuration contracts required to execute its pipeline.

#### Scenario: Configuration ownership is inspected
- **WHEN** a developer inspects the solution's summarization configuration types
- **THEN** the sole concrete summarization configuration class is in the CLI project and neither framework nor sample plugin defines it

### Requirement: Plugin deployment SHALL remain host-operator managed
The sample plugin SHALL own its active settings, environment settings, launch profile, Dockerfile, executable startup, and deployment output. Its build SHALL not copy artifacts into a separate API host or plugin directory.

#### Scenario: Plugin build does not alter host plugin directories
- **WHEN** the sample plugin is built
- **THEN** no build configuration places its artifacts in another host's output or a `Plugins/` directory

#### Scenario: Plugin loads base and environment settings
- **WHEN** the sample plugin starts in a named environment
- **THEN** it loads its base settings and overlays its available environment-specific settings before registering custom dependencies and Runtime services

#### Scenario: Plugin image is built
- **WHEN** an operator builds the sample plugin Dockerfile
- **THEN** the resulting image runs the sample plugin executable as the web service

### Requirement: Pipeline plugins SHALL not depend on host-owned conversation state
Chat and summarization pipeline plugins SHALL receive conversation data through their existing initialization inputs and SHALL NOT require a host-owned conversation context or a stateful application runner.

#### Scenario: Plugin pipelines run with caller-supplied context
- **WHEN** a host invokes a loaded chat or summarization pipeline with supplied conversation messages
- **THEN** the plugin processes those inputs without requiring access to conversation state retained by the host

### Requirement: Plugin documentation SHALL identify the framework package
Documentation for the default chat pipeline plugin SHALL identify `AgentMesh.Runtime` as the plugin-facing package, identify `AMCodePipeline` as the external reference project, and state that the plugin project itself is the host responsible for configuration, composition, HTTP execution, and deployment. It SHALL direct developers to the external project's repository for current run and Docker instructions.

#### Scenario: Plugin author selects dependencies
- **WHEN** a plugin author reads the default pipeline plugin documentation
- **THEN** they can identify the Runtime package to target, the plugin-owned files required to run and deploy a pipeline service, and the external repository that demonstrates them
