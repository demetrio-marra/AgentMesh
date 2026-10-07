## 1. Dialog Structure and Styling

- [x] 1.1 Add an accessible parameter-detail dialog, title, close control, and filtered-table host beside the existing Diagnostics modal in `AgentMeshWeb/Views/Chat/Index.cshtml`, and verify the rendered page exposes the dialog label and controls only when the detail view is opened.
- [x] 1.2 Add responsive nested-overlay, dialog, row-activation, focus, and scroll-container styles in `AgentMeshWeb/wwwroot/css/site.css`, and verify desktop and narrow viewports keep the detail dialog centered and its table scrollable within the viewport.

## 2. Parameter Detail Behavior

- [x] 2.1 Refactor `AgentMeshWeb/wwwroot/js/chat-diagnostics.js` to derive the ordered parameter-by-step matrix data once and render both the full matrix and a parameter-filtered table from it; verify matching values, blank cells, and event ordering remain identical in both views.
- [x] 2.2 Add keyboard-accessible parameter-row activation and controller-local selected-parameter state, and verify selecting a row opens the filtered dialog while live diagnostic events refresh the displayed selected row.
- [x] 2.3 Implement explicit-close and detail-backdrop dismissal handlers that preserve the parent Diagnostics modal, selected `Parameters by Step` tab, matrix, and session diagnostics; verify the parent modal's backdrop still does not close it and a new request clears the detail selection.
- [x] 2.4 Filter parameter-detail columns to steps with non-empty values for the selected parameter while preserving source order; verify the full matrix still shows every step and blank cells.

## 3. Verification

- [x] 3.1 Build `AgentMeshWeb` and resolve diagnostics introduced by the Razor, JavaScript, or CSS changes.
- [x] 3.2 Manually exercise a multi-step request with several parameter diffs on desktop and narrow viewports; verify row filtering, long-value scrolling, close-control dismissal, detail-backdrop dismissal, parent-modal preservation, and live updates while detail is open.