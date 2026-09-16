## Purpose

Provide distinct, predictable console and HTTP entry points so users select the required runtime through the launch target rather than runtime mode flags.

## ADDED Requirements

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