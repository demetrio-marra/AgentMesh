# stateless-pipeline-runner Specification

## Purpose
Provide one stateless application-layer runner that executes caller-supplied chat and summarization work without retaining conversation state between executions.

## Requirements

### Requirement: Application pipeline execution SHALL be stateless
The application layer SHALL expose one runner for both chat-request and summarization pipeline execution. Each execution SHALL receive the relevant conversation messages from its caller, SHALL use those messages only for that execution, and SHALL NOT retain, append, summarize, or mutate conversation state for later executions.

#### Scenario: Independent chat executions use caller-owned context
- **WHEN** two chat executions are submitted with different conversation message sequences
- **THEN** each execution uses only its supplied sequence and no message from either execution is retained for the other

#### Scenario: Summarization receives caller-owned context
- **WHEN** a summarization execution is submitted with a language and conversation messages
- **THEN** the runner initializes and executes the summarization pipeline from those supplied values without retaining the resulting summary or source messages

### Requirement: The unified runner SHALL preserve pipeline-specific selection rules
The runner SHALL select chat pipelines by the existing optional name-selection rules and SHALL require exactly one available summarization pipeline without accepting a summarization pipeline name.

#### Scenario: Multiple chat pipelines require explicit selection
- **WHEN** a chat execution is submitted without a name and multiple chat pipelines are available
- **THEN** the execution is rejected before pipeline work begins

#### Scenario: Summarization configuration is ambiguous
- **WHEN** a summarization execution is submitted and zero or multiple summarization pipelines are available
- **THEN** the execution is rejected before pipeline work begins

### Requirement: Stateless-runner documentation SHALL identify framework ownership
Documentation for stateless pipeline execution SHALL identify `AgentMesh.Application` as the reusable framework package that provides pipeline execution for class-library plugins, with `AgentMesh.Api` hosting custom pipelines at runtime.

#### Scenario: Contributor reviews stateless execution
- **WHEN** a contributor reads stateless pipeline-runner behavior
- **THEN** they can identify the reusable framework boundary separately from the executable pipeline host
