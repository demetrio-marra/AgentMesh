## ADDED Requirements

### Requirement: CLI startup SHALL use the API configuration summary
The CLI SHALL retrieve its startup display data from the authenticated API configuration summary endpoint instead of reading server runtime configuration locally.

#### Scenario: Configuration summary succeeds
- **WHEN** the CLI starts and the API returns an authorized configuration summary
- **THEN** the CLI displays the sandbox, user agent, and configured agent information from that response

#### Scenario: Configuration summary is unavailable
- **WHEN** the API rejects or cannot serve the configuration summary
- **THEN** the CLI reports that startup configuration could not be retrieved and does not display locally sourced server configuration
