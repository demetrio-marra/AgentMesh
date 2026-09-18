# default-chat-pipeline-plugin Specification

## Purpose

Provide complete default chat and summarization pipelines as an independently consumable plugin example for AgentMesh plugin authors.

## Requirements

### Requirement: Complete default pipeline sample plugin SHALL be provided
The distribution SHALL include a class-library sample plugin that preserves the default chat request and conversation-summarization pipelines' externally observable behavior. The plugin SHALL include the plugin-owned parameters, steps, concrete agents, prompt content, models, helpers, utilities, configuration, and exceptions needed to run both pipelines.

#### Scenario: Default pipeline is registered from the sample plugin
- **WHEN** the sample plugin assembly is present in the configured plugin directory before host startup
- **THEN** the host discovers its bootstrap registration and exposes a chat pipeline named `default`

#### Scenario: Default pipeline request behavior is retained
- **WHEN** a request is routed to the sample plugin's `default` pipeline
- **THEN** it follows the same request-analysis, knowledge, task-execution, and response-composition branches as the prior default pipeline

#### Scenario: CLI user explicitly summarizes conversation context
- **WHEN** an interactive CLI user enters `/summarize`
- **THEN** the host invokes the sample plugin's summarization pipeline for the current in-memory conversation and replaces the summarized messages with its returned summary

#### Scenario: CLI starts without a summarization pipeline
- **WHEN** the CLI frontend starts and no loaded plugin provides an `ISummarizationPipeline`
- **THEN** startup fails with an error that identifies the required plugin-provided summarization pipeline

#### Scenario: Reaching the threshold does not summarize automatically
- **WHEN** an interactive-mode conversation context reaches or exceeds the configured summarization threshold during a normal chat request
- **THEN** the host retains the context without invoking the summarization pipeline until the user enters `/summarize`

#### Scenario: API mode keeps caller-owned context
- **WHEN** an API client sends a request with optional conversation messages
- **THEN** the host passes that context to the selected chat pipeline without retaining server-side context or invoking the summarization pipeline

### Requirement: The sample plugin SHALL rely on host-provided framework services
The sample plugin SHALL consume agent memory, knowledge-store, reranker, and code-sandbox capabilities only through framework service contracts supplied by the host. It SHALL not include replacement implementations for those services.

#### Scenario: Host provides infrastructure dependencies
- **WHEN** the host registers its configured infrastructure service implementations and loads the sample plugin
- **THEN** the plugin resolves its required framework service contracts and processes requests without embedding infrastructure adapters

### Requirement: The sample plugin SHALL compile against published framework packages
The sample plugin project SHALL reference required AgentMesh framework APIs through NuGet package references and SHALL not directly reference the `AgentMesh.Application` project.

#### Scenario: Plugin project has no direct application-project dependency
- **WHEN** a developer inspects the sample plugin project references
- **THEN** AgentMesh framework dependencies are NuGet package references and no project reference targets `AgentMesh.Application`

### Requirement: Concrete summarization configuration SHALL be CLI-owned
The concrete configuration class that defines the summarization language and retention policy SHALL be owned by the CLI project. Framework and plugin projects SHALL not define that concrete configuration class; the plugin SHALL consume only framework configuration contracts required to execute its pipeline.

#### Scenario: Configuration ownership is inspected
- **WHEN** a developer inspects the solution's summarization configuration types
- **THEN** the sole concrete summarization configuration class is in the CLI project and neither framework nor sample plugin defines it

### Requirement: Plugin deployment SHALL remain host-operator managed
Building the sample plugin SHALL not automatically copy, publish, or configure its output into a CLI or API host plugin directory.

#### Scenario: Plugin build does not alter host plugin directories
- **WHEN** the sample plugin is built
- **THEN** no build configuration places its artifacts in either host's `bin/Plugins` directory

### Requirement: Pipeline plugins SHALL not depend on host-owned conversation state
Chat and summarization pipeline plugins SHALL receive conversation data through their existing initialization inputs and SHALL NOT require a host-owned conversation context or a stateful application runner.

#### Scenario: Plugin pipelines run with caller-supplied context
- **WHEN** a host invokes a loaded chat or summarization pipeline with supplied conversation messages
- **THEN** the plugin processes those inputs without requiring access to conversation state retained by the host

### Requirement: Plugin documentation SHALL identify the framework package
Documentation for the default chat pipeline plugin SHALL identify `AgentMesh.Application` as the reusable plugin-facing framework package, with `AgentMesh.Api` as the host that runs deployed custom pipelines.

#### Scenario: Plugin author selects dependencies
- **WHEN** a plugin author reads the default pipeline plugin documentation
- **THEN** they can identify the framework package to target and the host responsible for pipeline execution