## ADDED Requirements

### Requirement: Web frontend SHALL show terminal request toast notifications
When an active chat or automatic summarization request reaches a terminal result, the web frontend SHALL display one in-app toast in the upper-right viewport. A successful terminal result SHALL display `Request completed` with a success visual treatment; a failed terminal result SHALL display `Request failed` with an error visual treatment. Each toast SHALL provide an accessible close control.

The toast SHALL remain until the user closes it or it has been visible in a focused application tab for 10 cumulative seconds. Time while the document is hidden or the application tab is unfocused SHALL NOT count toward automatic dismissal. A cancelled request SHALL NOT produce a terminal request toast.

#### Scenario: Visible request completes successfully
- **WHEN** an active request completes while the application tab is visible and focused
- **THEN** the frontend displays a success toast reading `Request completed` in the upper-right viewport and automatically dismisses it after 10 seconds of visible, focused time unless the user closes it first

#### Scenario: Request fails while application is not being viewed
- **WHEN** an active request fails while the application tab is hidden or unfocused
- **THEN** the frontend displays an error toast reading `Request failed` when the application is next visible and does not count hidden or unfocused time toward its 10-second dismissal interval

#### Scenario: User dismisses a terminal toast
- **WHEN** the user activates a terminal toast's close control
- **THEN** the frontend removes that toast immediately and does not show it again for the same terminal event

#### Scenario: User cancels a request
- **WHEN** the user cancels an active chat or summarization request
- **THEN** the frontend does not display a `Request completed` or `Request failed` toast for the cancellation

### Requirement: Web frontend SHALL request and use desktop notification permission
On startup, when the browser supports desktop notifications and permission has not already been granted or denied, the web frontend SHALL request the user's permission to show desktop notifications. When permission is granted, the frontend SHALL issue one desktop notification for each active chat or automatic summarization request that completes or fails while the application is minimized or not visible. Successful notifications SHALL read `Request completed`; failed notifications SHALL read `Request failed`.

The frontend SHALL NOT issue desktop notifications while the application is visible, after permission is denied, when the browser does not support notifications, or for cancelled requests. Permission failure or lack of browser support SHALL NOT interrupt the chat experience or prevent in-app terminal notifications.

#### Scenario: Startup permission is granted
- **WHEN** the application starts in a browser that supports desktop notifications and notification permission has not been decided
- **THEN** the browser is asked for permission to enable desktop notifications

#### Scenario: Hidden request completes after permission is granted
- **WHEN** an active request completes while the application is minimized or not visible and desktop notification permission is granted
- **THEN** the frontend shows one desktop notification reading `Request completed`

#### Scenario: Hidden request fails without permission
- **WHEN** an active request fails while the application is not visible and desktop notification permission is denied or unavailable
- **THEN** the frontend does not show a desktop notification and continues its ordinary terminal handling

#### Scenario: Visible request terminates
- **WHEN** an active request completes or fails while the application is visible
- **THEN** the frontend does not show a desktop notification

### Requirement: Web frontend SHALL temporarily annotate an unfocused tab title
When an active request completes or fails while its application document is hidden because the user has selected another browser tab, the frontend SHALL temporarily set its document title to the default title followed by ` - Request completed` or ` - Request failed`, respectively. When that application tab next receives focus, the frontend SHALL restore the default document title.

The frontend SHALL NOT alter the title for a visible terminal result, a minimized browser window, a cancellation, or after title restoration. Title annotation SHALL NOT change the application title persisted outside the loaded page.

#### Scenario: Request completes in another browser tab
- **WHEN** an active request completes while the browser window is focused but a different tab is selected
- **THEN** the application tab title becomes its default title followed by ` - Request completed`

#### Scenario: Request fails in another browser tab
- **WHEN** an active request fails while the browser window is focused but a different tab is selected
- **THEN** the application tab title becomes its default title followed by ` - Request failed`

#### Scenario: User returns to the application tab
- **WHEN** the application tab has a terminal title annotation and receives focus
- **THEN** the frontend restores the default document title