## Why

`ConfigurationController.Get` currently maps configuration records and assembles the API response itself. This couples the HTTP adapter to application business logic and makes the controller harder to keep "dumb" as the configuration summary evolves. Move that mapping into `AppInstance` while preserving the existing endpoint contract and explicitly excluding tests from this change.

## What Changes

- Add an application-layer operation on `AppInstance` that builds the configuration summary from the existing sandbox, user, and agent configuration inputs.
- Update `ConfigurationController` to delegate summary creation to `AppInstance` and return the resulting response.
- Remove configuration mapping and temperature conversion responsibilities from the controller.
- Preserve the existing route, authentication, response schema, sensitive-field exclusions, and runtime behavior.

## Capabilities

### New Capabilities

None. This is an implementation-only layering refactor.

### Modified Capabilities

None. The existing API configuration-summary requirements remain unchanged.

## Impact

Affected areas are `AgentMesh.Application.Services.AppInstance` and `AgentMesh.Api.Controllers.ConfigurationController`, plus their dependency injection wiring if needed to expose the application operation. No API endpoint, DTO contract, configuration source, or external dependency changes. Tests are explicitly out of scope.

## Non-goals

- Changing the configuration summary endpoint, authorization, or response payload.
- Changing configuration visibility or secret filtering.
- Refactoring unrelated controllers or application services.
- Adding or modifying tests.