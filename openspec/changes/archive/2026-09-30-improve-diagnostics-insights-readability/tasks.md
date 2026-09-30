## 1. Streamed diagnostics data

- [x] 1.1 Relay workflow-error events through the existing web progress path with their original raw payload before preserving the current operation failure behavior; verify the client retains the error payload and the browser still displays the workflow error.

## 2. Browser diagnostics model

- [x] 2.1 Extend the existing accumulated diagnostics event handling to derive arrival-ordered step records from start, completion, and error events while preserving the raw chunk list unchanged; verify started, completed, and malformed optional-field payloads render without client errors.
- [x] 2.2 Implement selected-step rendering with display-safe two-column input and output grids, incomplete and error states, and human-readable elapsed time; verify code, agentic, in-progress, and failed step examples.

## 3. Diagnostics modal interface

- [x] 3.1 Replace the single-panel modal markup with accessible `Steps details` and `Raw data` tabs, selecting Steps details whenever the modal opens while retaining the existing raw textarea in the final tab; verify keyboard and click tab selection and explicit close behavior.
- [x] 3.2 Add enlarged, viewport-bounded responsive styling for the summary/detail split and independently scrollable parameter grids without disturbing the Raw data textarea; verify desktop and narrow viewport layouts with long names and values.

## 4. Integration validation

- [x] 4.1 Build AgentMesh.Runtime and AgentMeshWeb, then exercise streamed chat and summarization diagnostics to confirm event ordering, session isolation, request-scoped Raw data, Step details derived from raw diagnostics, step-detail rendering, partial-step updates, and failure presentation.
- [x] 4.2 Run `openspec validate improve-diagnostics-insights-readability --strict` and verify all change artifacts validate successfully.