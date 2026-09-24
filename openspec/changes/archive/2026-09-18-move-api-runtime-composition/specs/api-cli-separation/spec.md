## MODIFIED Requirements

### Requirement: Executable hosts are build-independent

The API and console executable projects SHALL build and run without a project reference to each other. The `AgentMesh` DLL SHALL be a library shared by the API and Application DLLs. Each plugin DLL SHALL reference the Application DLL through its NuGet package. The API SHALL not reference the `AgentMesh.Framework.Application` package directly. The API SHALL discover configured plugin assemblies, invoke each bootstrap registration with the API host's `IConfiguration`, and load plugin dependencies without duplicating assemblies already loaded by the host. API-only and console-only assets and registrations SHALL remain owned by their respective hosts.

#### Scenario: Build the API independently

- **WHEN** a developer restores and builds the API project
- **THEN** the build succeeds without compiling or referencing the console executable project or directly referencing the `AgentMesh.Framework.Application` package

#### Scenario: Build the console independently

- **WHEN** a developer restores and builds the console project
- **THEN** the build succeeds without compiling or referencing the API executable project

#### Scenario: Restore a plugin dependency graph

- **WHEN** a developer restores a plugin DLL
- **THEN** the plugin references the Application DLL through its NuGet package, and the API and Application DLLs both consume the shared `AgentMesh` library

#### Scenario: Start the API with a plugin

- **WHEN** the API starts with a valid environment configuration and a valid plugin
- **THEN** the API passes its configuration to the plugin bootstrap and can execute the pipeline registered by that bootstrap without directly referencing the Application package

#### Scenario: Start the API with plugins sharing host dependencies

- **WHEN** the API discovers plugins whose dependencies include an assembly already loaded by the host
- **THEN** the plugin load context reuses the host assembly, preserving shared type identity without loading a duplicate assembly

#### Scenario: Start either host with shared runtime assets

- **WHEN** either executable starts with a valid environment configuration
- **THEN** it loads the shared configuration and prompt assets required by its supported workflows without retrieving them from the other executable's output