## 1. Reshape the chat and diagnostics surfaces

- [x] 1.1 Update the chat Razor view to remove the configuration rail and header control, add the rightmost accessible `Configuration` diagnostics tab and panel, and remove standalone progress and error viewport elements; verify the rendered DOM contains the four-tab dialog and no configuration rail.
- [x] 1.2 Move the existing sanitized configuration summary and unavailable-state markup into the new Configuration panel; verify both a populated summary and configuration retrieval failure remain non-blocking.
- [x] 1.3 Update responsive CSS for the single-column chat workspace, lower-right message copy actions, distinct pending and error assistant treatments, and a full-height Raw data tab panel; verify desktop and narrow viewport layouts have no overlapping controls or unused raw-textarea space.

## 2. Implement accepted-request transcript lifecycle

- [x] 2.1 Refactor browser chat state so a temporary user message and assistant placeholder are created only after SignalR `Submit` succeeds; verify a rejected submission does not add a placeholder.
- [x] 2.2 Implement the placeholder renderer and timer with italic `Processing`, current step text, human-readable elapsed duration since the last state transition, and timer cleanup; verify arrival of initial and later progress events updates and resets the counter without creating duplicate placeholders.
- [x] 2.3 Reconcile the placeholder with terminal SignalR events: replace it with normal server-backed messages on success, a reddish `An error has occurred` message on failure, and appropriate cleanup on cancellation, New chat, and reconnect; verify errors and transient messages never enter the next request's conversation context and no retry control appears.
- [x] 2.4 Centralize terminal composer cleanup and invoke it after success, failure, and cancellation; verify the input regains focus in each case and browser leave confirmation uses `The conversation will be lost. Continue?`.

## 3. Complete diagnostics interaction updates

- [x] 3.1 Extend diagnostics tab selection, ARIA state, focus handling, and arrow-key navigation to include the rightmost Configuration tab; verify each tab can be selected by mouse and keyboard while an active workflow continues.
- [x] 3.2 Retain raw-event collection and step-summary behavior while removing ordinary viewport progress and error rendering; verify Raw data and Steps details still show the active request's events and errors.

## 4. Verify the web experience

- [x] 4.1 Build AgentMeshWeb with `dotnet build AgentMeshWeb/AgentMeshWeb.csproj` and resolve any compilation or static-asset errors.
- [x] 4.2 Exercise successful, failed, cancelled, and rejected SignalR submission flows in the running web app; verify placeholder placement, state transitions, focus restoration, persisted-context isolation, copy-button placement, and absence of retry UI.
- [x] 4.3 Exercise Diagnostics and responsive layouts at desktop and narrow widths; verify Configuration is rightmost, Raw data fills its panel, keyboard tab navigation works, and configuration no longer reserves chat viewport space.