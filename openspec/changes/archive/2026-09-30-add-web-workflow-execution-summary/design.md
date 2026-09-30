## Context

See `proposal.md` for motivation and `specs/web-chat-frontend/spec.md` for the behavior contract. AgentMeshWeb currently retains raw lifecycle events in the browser for Steps details and Raw data, but its terminal `WorkflowResult` model keeps only conversation counters. The existing API terminal response already includes ordered main-pipeline step statistics and itemized agent execution costs. AgentMeshCLI consumes those fields in `ConsoleHelper.PrintTokenUsageSummary` after a successful chat response and does not print an equivalent summary for automatic summarization.

The feature is limited to AgentMeshWeb. Framework, runtime, application, and infrastructure project behavior and contracts remain unchanged.

## Goals / Non-Goals

**Goals:**
- Deliver the existing successful chat workflow result to the requesting browser session for diagnostics-only presentation.
- Reproduce the CLI's token-priced and hourly-priced accounting classification, totals, and row-to-cost matching in an accessible HTML table.
- Keep Summary independently derived from the terminal result while preserving raw lifecycle events as the canonical source for Raw data and Steps details.

**Non-Goals:**
- Create or alter API endpoints, stream event payloads, callback payloads, pricing configuration, or server-side persistence.
- Show an itemized summary for automatic summarization, which has no equivalent terminal workflow-result payload.
- Change the existing conversation state contract or retain diagnostics across browser reloads.

## Decisions

### Extend only AgentMeshWeb's terminal-result representation and session relay

AgentMeshWeb will add local DTOs for the step statistics and agent execution costs already present in the successful chat terminal result. Once `ChatCoordinator` receives that result and verifies the operation remains current, it will send a dedicated diagnostics-summary SignalR message to the owning chat group before continuing its existing context-save and optional summarization flow. The browser will retain that payload in request-scoped memory and redraw Summary when it arrives.

This keeps sensitive API credentials server-side and does not require an API, runtime, or framework change. It also prevents terminal accounting data from being manufactured from incomplete lifecycle events.

Alternative considered: derive the table only from raw progress events. Rejected because those events intentionally omit token counts and itemized costs.

Alternative considered: put summary data into the ordinary chat state. Rejected because workflow accounting is transient diagnostics data, not conversation context, and should not be replayed as part of state initialization.

### Mirror the CLI's deterministic accounting logic in browser rendering

The browser will walk main-pipeline statistics in their received order. Non-agentic steps stay in the token-priced table without cost data. For agentic steps, cost entries will be grouped by agent name and consumed from per-agent queues in order, matching `ConsoleHelper.PrintTokenUsageSummary`; an hourly-priced match moves that row into the hourly section, while an unmatched agentic row remains token-priced with zero or unavailable cost display as appropriate. Token totals and percentages will include token-priced agentic rows only. Monetary values will calculate from the terminal item's token pricing or elapsed hourly rate and use a fixed decimal display suitable for USD. Beneath the step-consumption grid, the renderer will show the accumulated elapsed duration across all step statistics and the combined token-priced and hourly-priced cost.

Alternative considered: match costs by array index. Rejected because non-agentic steps do not have cost entries and repeated agent names need queue ordering.

### Keep modal tab behavior accessible and responsive

The Razor modal will prepend a Summary tab and panel ahead of the existing Step details and Raw data tabs. The existing tab-selection helper will manage all three tabs, select Summary and focus its tab when opening, and preserve keyboard traversal. Summary will render DOM text nodes and semantic table headers. CSS will bound the panel within the existing dialog, permit horizontal scrolling for the wide token table, retain readable column alignment, and preserve the current narrow-viewport modal layout.

Alternative considered: collapse costs into the Steps details view. Rejected because the workflow-wide totals and cross-step comparison need a stable tabular view and the requested first tab is Summary.

### Reset summary with the existing request-scoped diagnostics lifecycle

`resetDiagnostics` will clear terminal summary state together with raw events and selected step before a new chat submission. Until a successful terminal chat result is received, Summary will show an explicit empty state. A failed or canceled request will therefore retain no stale completed summary from a prior request.

Alternative considered: preserve the prior summary until a new one completes. Rejected because it would mix a prior execution with the current request's raw diagnostics and steps.

## Risks / Trade-offs

- [A wide table can exceed narrow viewport width] -> Place the table in an independently horizontally scrollable panel and keep existing modal viewport bounds.
- [Repeated agent roles can have multiple executions] -> Pair statistics and cost entries through per-agent first-in-first-out queues, as the CLI does.
- [Terminal result arrives before the browser opens Diagnostics] -> Retain the summary in page-memory for the active request and render it on open.
- [Automatic summarization follows a chat workflow] -> Keep the completed chat summary visible until the next chat submission; summarization lifecycle events remain available through Steps details and Raw data.

## Migration Plan

1. Deploy the AgentMeshWeb-only DTO, coordinator relay, markup, browser, and CSS changes together.
2. Build AgentMeshWeb and exercise successful token-priced, non-agentic, hourly-priced, failed, canceled, and automatic-summarization chat flows at desktop and narrow viewport sizes.
3. Roll back by reverting the AgentMeshWeb changes; no persisted data, API, or framework migration is required.

## Open Questions

None.