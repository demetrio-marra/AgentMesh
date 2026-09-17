## Purpose

Provide distinct, predictable console and HTTP entry points so users select the required runtime through the launch target rather than runtime mode flags.

## Requirements

### Requirement: Console entry point is console-only

The console entry point SHALL start the interactive workflow directly and SHALL NOT select between console and HTTP hosting based on a command-line mode switch.

#### Scenario: Launch the console profile

- **WHEN** the `Interactive` launch profile starts the console project
- **THEN** the interactive user input workflow starts without requiring `--interactive`

#### Scenario: Pass the former mode switch to the console

- **WHEN** the console executable is started with `--interactive`
- **THEN** it SHALL NOT start an HTTP host or use the switch to select an alternate runtime

### Requirement: API entry point is web-only

The API entry point SHALL host the existing authenticated HTTP API and SHALL execute requests through the stateless application path.

#### Scenario: Launch the web profile

- **WHEN** the `Web` launch profile starts the API project
- **THEN** the HTTP host exposes the existing API routes and Swagger documentation

#### Scenario: Process an authenticated request

- **WHEN** a client sends a valid API-key-authenticated request using an existing request route
- **THEN** the API returns the existing workflow result contract and preserves named and default pipeline routing behavior

### Requirement: Launch profiles select the host

The development launch configuration SHALL expose `Interactive` for the console project and `Web` for the API project.

#### Scenario: Select Interactive

- **WHEN** a developer selects `Interactive`
- **THEN** the console project launches as the active target

#### Scenario: Select Web

- **WHEN** a developer selects `Web`
- **THEN** the API project launches as the active target and opens its web documentation entry point

### Requirement: Shared runtime configuration remains compatible

Both entry points SHALL use the existing application, infrastructure, plugin, and configuration behavior required to execute the same pipelines, while API-only settings and components remain owned by the API entry point.

#### Scenario: Run with existing configuration

- **WHEN** either host is started with a valid existing environment configuration
- **THEN** its supported workflow can initialize without requiring the other host project to run

### Requirement: Executable hosts are build-independent

The API and console executable projects SHALL build and run without a project reference to each other. Shared configuration loading, service registration, and runtime assets required by both hosts SHALL be owned by non-executable application or framework layers; API-only and console-only assets and registrations SHALL remain owned by their respective hosts.

#### Scenario: Build the API independently

- **WHEN** a developer restores and builds the API project
- **THEN** the build succeeds without compiling or referencing the console executable project

#### Scenario: Build the console independently

- **WHEN** a developer restores and builds the console project
- **THEN** the build succeeds without compiling or referencing the API executable project

#### Scenario: Start either host with shared runtime assets

- **WHEN** either executable starts with a valid environment configuration
- **THEN** it loads the shared configuration and prompt assets required by its supported workflows without retrieving them from the other executable's output

### Requirement: Console hosting SHALL be an API frontend
The console entry point SHALL own only interactive input, in-memory conversation state, HTTP transport, callback reception, and console presentation; pipeline execution and runtime service configuration SHALL remain owned by the API entry point.

#### Scenario: Console starts independently of pipeline runtime
- **WHEN** the CLI starts with a reachable API configuration
- **THEN** it can initialize its interactive frontend without loading the API pipeline implementation locally

### Requirement: Runtime configuration SHALL be merged into API-owned files
The API SHALL own configuration files containing the shared runtime sections currently supplied by the CLI, while preserving API-only authentication and hosting sections. The CLI SHALL retain only settings required to connect to the API, receive callbacks, and manage local conversation summarization. This relocation SHALL NOT change API endpoints or processing behavior.

#### Scenario: API starts from its own configuration
- **WHEN** the API is deployed without the CLI project files
- **THEN** it loads the merged shared runtime configuration and its API-only settings from its own files

#### Scenario: API-only configuration is preserved
- **WHEN** shared CLI configuration is merged into API configuration
- **THEN** API authentication and hosting settings remain present and effective

#### Scenario: API behavior remains unchanged
- **WHEN** the configuration files are relocated and merged
- **THEN** existing API routes, authentication behavior, callbacks, and pipeline processing remain unchanged
