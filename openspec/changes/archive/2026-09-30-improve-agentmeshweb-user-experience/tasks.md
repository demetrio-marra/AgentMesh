## 1. Viewport Layout and Display

- [x] 1.1 Update the chat view's initial empty-state text to `Conversation is empty`.
- [x] 1.2 Bound the application shell and responsive workspace to the viewport and preserve independent transcript and configuration-rail vertical scrolling.

## 2. Chat Interaction Behavior

- [x] 2.1 Centralize transcript bottom scrolling and update client rendering to retain it after pending, state-refresh, and summary-render paths; format cumulative cost with two decimal places.
- [x] 2.2 Add textarea keyboard handling so Enter submits an eligible message while Shift+Enter and Ctrl+Enter insert a newline, including IME-safe behavior.
- [x] 2.3 Track visible conversation content and require confirmation before New chat clears a non-empty chat; register the browser leave-warning handler for refresh and navigation.