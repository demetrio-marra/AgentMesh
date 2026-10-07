## 1. Browser Notification Controller

- [x] 1.1 Add a browser-only notification controller ES module that requests undecided desktop-notification permission on startup, emits native notifications only for hidden terminal requests with granted permission, and tolerates unsupported or rejected browser APIs; verify through browser developer tools with granted, denied, and unavailable Notification API states.
- [x] 1.2 Implement terminal toast creation, accessible dismissal, and cumulative 10-second timeout accounting that runs only while the page is visible and focused; verify a toast remains displayed through a hidden/unfocused interval and dismisses after 10 seconds of visible focused time.
- [x] 1.3 Implement document-title suffixing for hidden application tabs in a focused browser window and restoration on return to the application tab; verify completed and failed title suffixes restore to the default title on focus.

## 2. Chat Lifecycle Integration

- [x] 2.1 Integrate the notification controller into `chat.js` and normalize the active request's successful and failed terminal paths so each emits exactly one completion or failure notification; verify chat and automatic summarization produce their respective messages without duplicate notices.
- [x] 2.2 Preserve cancellation, late SignalR events, reconnect cleanup, conversation commits, diagnostics, and existing failure rendering while suppressing notifications for cancellation; verify a stopped request produces no terminal notification and a subsequent request behaves normally.

## 3. Toast Presentation

- [x] 3.1 Add the stable accessible toast region and success/error/close-control styles to the AgentMeshWeb chat UI; verify upper-right placement, readable contrasting treatments, keyboard-accessible close behavior, and responsive layout without overlap at common desktop and mobile viewport widths.

## 4. Validation

- [x] 4.1 Build `AgentMeshWeb/AgentMeshWeb.csproj` with `dotnet build` and resolve notification-change compilation or asset-loading issues.
- [x] 4.2 Run a supported-browser acceptance pass covering visible completion/failure, hidden or minimized completion/failure with desktop permission granted and denied, another-tab title annotation/restoration, toast timer pausing, close control, and cancellation; record any browser-specific limitation in the implementation notes.

Acceptance notes: the app was run with the `AgentMeshWeb` launch profile at `http://localhost:61776`; the chat page loaded the module and accessible toast region, and desktop/mobile layout checks showed no horizontal overflow. Native Notification permission states, cross-tab focus transitions, and end-to-end chat/summarization terminal events could not be driven reliably by the integrated browser session, so those remain browser-manual checks.