# systemone-infrastructure Specification

## Purpose

Provide a standalone, provider-configurable, typed text-only SystemOne connector with matching public Contracts definitions.

## Requirements

### Requirement: Configurable standalone SystemOne connector
The system SHALL provide a standalone SystemOne infrastructure integration whose HTTP transport DTOs/models reside in the Infrastructure project and whose matching public request/result/usage models and service interface reside in the Contracts project. It SHALL call only `POST /v1/systemone`, using configurable base URL, bearer API key, model, and positive timeout, without a system prompt, temperature, or LLM configuration dependency.

#### Scenario: Compatible provider endpoint
- **WHEN** an operator configures a compatible provider base URL and credentials
- **THEN** evaluation sends one authenticated request to that base URL's `v1/systemone` route and does not contact TypeSafe or a model-discovery endpoint

#### Scenario: Base URL contains a path prefix
- **WHEN** the base URL is `https://provider.example/api` or `https://provider.example/api/`
- **THEN** the request targets `https://provider.example/api/v1/systemone` in both cases

#### Scenario: Invalid configuration
- **WHEN** the connector is first resolved with an invalid absolute HTTP(S) URL, missing key/model, or nonpositive timeout
- **THEN** it reports an explicit configuration error before sending a request

### Requirement: Typed mixed text decisions
The connector SHALL support named Noul, Choice, and Score questions together in one JSON request. State SHALL preserve string, object, or array JSON values. Optional instructions SHALL support these shapes and null/omission; Noul true/false criteria and Choice descriptions SHALL additionally support null, and Score descriptions SHALL preserve string, object, or array values. Choice criteria SHALL contain 1-255 entries and Score criteria SHALL contain 1-10 ordered levels.

#### Scenario: Mixed request
- **WHEN** a caller submits named Noul, Choice, and Score questions with nested structured values
- **THEN** their JSON shapes and names are preserved and each answer is returned under its corresponding question name

#### Scenario: Optional Noul instructions
- **WHEN** a caller submits a Noul with omitted instructions and valid criteria
- **THEN** the request remains valid and the connector does not invent instructions or a prompt

#### Scenario: Invalid or unsupported input
- **WHEN** a caller supplies empty questions, scalar/null state, out-of-range criteria counts, unsupported question types, or image/asset payloads
- **THEN** the connector reports a validation error before HTTP execution and never sends an image or long-context opt-in header

### Requirement: Preserve documented answer semantics and usage
The connector SHALL return the response model, named typed answers, and required nonnegative input/output token counts. Noul SHALL preserve yes probability; Choice SHALL preserve selection, confidence, and named probabilities; Score SHALL preserve fractional expected score, confidence, legend, and level probabilities. Probability and confidence values SHALL be finite and within 0-1. Answer names and types SHALL match the request; selected choices and returned probability/legend keys SHALL match its criteria.

#### Scenario: Fractional score and actual model
- **WHEN** a model alias request returns a versioned model, a score of 1.7, its legend/probabilities, and usage
- **THEN** the connector preserves the actual model, fractional score, legend/probabilities, and both token counts without rounding or substituting the alias

#### Scenario: Malformed successful response
- **WHEN** a successful HTTP response has missing usage, missing required answer fields, unknown discriminators, invalid ranges, or mismatched answer names/types/criteria
- **THEN** the connector reports an explicit response-contract failure instead of returning empty answers or fabricated usage

### Requirement: Explicit failure and cancellation behavior
HTTP errors, transport failures, timeouts, cancellation, and malformed JSON SHALL remain distinguishable failures. Provider 400/422 validation details SHALL be available through a sanitized error representation. Credentials and submitted state SHALL NOT appear in routine logs or public error messages. The connector SHALL NOT automatically replay billable evaluations.

#### Scenario: Provider validation or throttling
- **WHEN** the provider responds with 400, 422, or 429
- **THEN** the connector reports status and sanitized error information without returning success or retrying the request

#### Scenario: Cancellation
- **WHEN** the caller cancels a pending evaluation
- **THEN** HTTP execution and response reading respect the cancellation and no fabricated result is returned

### Requirement: Usage-only infrastructure boundary
The connector SHALL expose API-reported input/output token usage without monetary cost calculation, pricing configuration, or financial reporting. The integration SHALL NOT add an executor, pipeline step, parameter configuration, runtime registration, or change existing workflow statistics/cost aggregation.

#### Scenario: Direct service evaluation
- **WHEN** a consumer constructs the service with a valid configuration and evaluates a request
- **THEN** the result contains the actual model, typed answers, and input/output usage but no monetary cost or price fields

#### Scenario: Existing framework remains unchanged
- **WHEN** the standalone connector and Contracts definitions are added to the solution
- **THEN** Application, Runtime, core statistics, existing pipeline scheduling, and workflow financial reporting are unchanged
