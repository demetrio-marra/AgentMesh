## ADDED Requirements

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