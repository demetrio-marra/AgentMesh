## 1. Relay Raw Progress Data

- [x] 1.1 Extend the AgentMeshWeb workflow-progress model and SSE stream parsing so every non-terminal lifecycle event retains its original raw JSON data while preserving the existing friendly progress message; verify by exercising representative workflow-started, step-started, and step-completed payloads and confirming their raw parameter, elapsed-time, token, and cost fields survive to the SignalR `Progress` event.
- [x] 1.2 Keep forwarding the enriched progress record through the existing chat-session SignalR group without altering terminal-result handling; verify by building `AgentMeshWeb` and confirming a second chat session cannot receive another session's progress or diagnostics payloads.

## 2. Add Diagnostics Viewer

- [x] 2.1 Add the Diagnostics top-bar control and modal markup with an explicit close control and a single read-only diagnostics textarea; verify the modal opens from the control, is centered and background-obscuring, and does not close on backdrop clicks.
- [x] 2.2 Update browser progress handling to append raw chunks in arrival order for the loaded page and render that ordered list in the diagnostics textarea without transforming prior chunks; verify chunks received while the modal is open append at the end and ordinary progress, chat submission, cancellation, and summarization behavior remain operational.
- [x] 2.3 Style the modal and textarea for approximately 80 percent viewport coverage, shadowed separation, and independent vertical scrolling across desktop and narrow mobile viewports; verify controls and chat content do not overlap or become inaccessible at both viewport sizes.

## 3. Validate Behavior

- [x] 3.1 Run `dotnet build AgentMeshWeb/AgentMeshWeb.csproj` and resolve diagnostics-related compilation errors.
- [x] 3.2 Manually run the web client against streamed chat and summarization workflows; verify raw diagnostics include incoming pipeline payloads, remain available after explicit close/reopen during the page lifetime, reset after page reload, and do not appear in the conversation transcript.