## Purpose

Define framework and host behavior for loading third-party pipeline plugins from startup-mounted DLLs, enabling multi-pipeline execution through a stable framework contract and NuGet-based extensibility.

## ADDED Requirements

### Requirement: Framework contracts SHALL be distributable as NuGet packages
The framework SHALL provide distributable package(s) containing the core contracts and immutable API DTO schema required to build plugin pipelines and steps.

#### Scenario: Plugin author builds against framework package
- **WHEN** a third-party developer creates a plugin project
- **THEN** they can reference the framework NuGet package(s) and implement framework contracts without modifying host code

### Requirement: Plugins SHALL self-register services through a bootstrap contract
The host SHALL support plugin bootstrap self-registration so plugin authors can explicitly register their pipelines, steps, parameters, and dependencies.

#### Scenario: Plugin registers services through bootstrap
- **WHEN** the host loads a plugin assembly at startup and finds a plugin bootstrap implementation
- **THEN** it invokes plugin registration to add only the services explicitly declared by the plugin

### Requirement: Host SHALL load plugins from the Plugins folder at startup
The host SHALL load plugin assemblies from a configured `Plugins` folder during container startup.

#### Scenario: Startup-time plugin discovery from mounted folder
- **WHEN** the service starts and plugin assemblies are present in the configured `Plugins` folder
- **THEN** the host loads those assemblies and makes plugin-registered pipelines available for request handling

#### Scenario: No runtime hot detection is required
- **WHEN** plugin DLLs are added or replaced after the service has started
- **THEN** those changes are not applied until the service is restarted/redeployed

### Requirement: Multiple chat pipelines SHALL be supported with unique names
The host SHALL support multiple `IChatRequestPipeline` implementations simultaneously, and each pipeline SHALL expose a unique `Name` used for routing.

#### Scenario: Pipeline names are resolved case-insensitively
- **WHEN** a client requests a named pipeline route using a different letter case
- **THEN** the host resolves the pipeline name successfully using case-insensitive matching

#### Scenario: Duplicate pipeline names invalidate plugin configuration
- **WHEN** multiple loaded pipelines expose the same `Name` under case-insensitive comparison
- **THEN** requests return a plugin configuration error response requiring redeploy, as defined by request access mode behavior