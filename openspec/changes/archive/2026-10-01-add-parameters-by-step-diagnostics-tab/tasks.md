## 1. Diagnostics Matrix

- [x] 1.1 Add the `Parameters by Step` tab and tab panel to the Diagnostics modal, extending click and arrow-key tab coordination; verify each Diagnostics tab can be selected and exposes its matching panel.
- [x] 1.2 Derive an ordered parameter-by-step matrix from the current request's step-started and step-completed lifecycle events, preserving first-diff row order and step-start column order; verify matching cells render `parametersDiff.newValue` and absent or incomplete values render blank.
- [x] 1.3 Refresh the matrix from appended diagnostic events and clear it through the existing new-request reset path; verify it updates during a workflow and contains no prior-request values after a subsequent submission.

## 2. Matrix Layout

- [x] 2.1 Style the matrix as a bounded table container with horizontal and vertical scrolling, wrapped/preserved long value text, and responsive modal constraints; verify multi-kilobyte values and many rows or columns remain inspectable without viewport overflow.

## 3. Validation

- [x] 3.1 Build `AgentMeshWeb/AgentMeshWeb.csproj` with `dotnet build` and manually verify a representative multi-step diagnostic stream for tab accessibility, ordering, blank cells, live updates, reset behavior, and two-axis scrolling.