## MODIFIED Requirements

### Requirement: Diagnostics SHALL show parameter history by workflow step
The Diagnostics modal SHALL include a keyboard-accessible `Parameters by Step` tab immediately after the `Steps details` tab. The tab SHALL show one row for every parameter present in a step-completed `parametersDiff` for the current request and one column for every started workflow step received for that request. The matrix SHALL be derived from the same session-scoped diagnostic lifecycle events used by existing Diagnostics views, without modifying the retained raw chunks or workflow processing.

Step columns SHALL be ordered by the arrival of their corresponding step-started events. Parameter rows SHALL be ordered by the first matching `parametersDiff` event in which the parameter appears. A cell SHALL display the `newValue` from the matching step-completed `parametersDiff` entry for its row parameter and column step, and SHALL be blank when that step has no matching diff entry. The matrix SHALL scroll horizontally and vertically within the available Diagnostics panel area. Parameter values, including multi-kilobyte text, SHALL remain inspectable without expanding the modal beyond its viewport bounds.

Each parameter row SHALL be selectable. Selecting a row SHALL open a viewport-centered parameter-detail dialog above the open Diagnostics modal. The dialog SHALL show the selected parameter's single row and only the steps where its value is non-empty, preserving those steps' source order and matching values. The complete source matrix SHALL continue to show every started step, including blank cells. The dialog SHALL provide an explicit close control and SHALL close when its own backdrop is clicked. Closing the detail dialog SHALL preserve the parent Diagnostics modal, its active tab, its diagnostics data, and the source matrix. The parent Diagnostics modal's backdrop behavior SHALL remain unchanged.

#### Scenario: User selects the parameter-history tab
- **WHEN** the Diagnostics modal is open and the user selects `Parameters by Step`
- **THEN** the tab becomes the selected Diagnostics view and displays a matrix derived from the latest request's received lifecycle events

#### Scenario: Workflow receives parameters across steps
- **WHEN** the browser receives step-started and step-completed events for multiple steps with parameter diffs that first appear at different times
- **THEN** columns appear in step-start arrival order, rows appear in parameter first-diff order, matching cells show each diff entry's `newValue`, and steps without a matching diff entry have blank cells

#### Scenario: Matrix content exceeds the panel bounds
- **WHEN** the parameter-history matrix has more rows, columns, or parameter-value text than fits in the Diagnostics panel
- **THEN** the matrix remains vertically and horizontally scrollable within the panel and the modal remains within the viewport

#### Scenario: User inspects one parameter across steps
- **WHEN** the user selects a parameter row in the parameter-history matrix
- **THEN** a viewport-centered detail dialog opens above Diagnostics with a one-row table for that parameter and only columns whose value for that parameter is non-empty, in source step order

#### Scenario: User closes parameter detail
- **WHEN** the user activates the parameter-detail dialog's explicit close control or clicks its backdrop
- **THEN** the parameter-detail dialog closes while the parent Diagnostics modal, active tab, and parameter-history matrix remain available

#### Scenario: A new request starts
- **WHEN** the browser session submits a new chat request after the prior request received diagnostic events
- **THEN** Parameters by Step clears the prior request's matrix and subsequently shows only lifecycle events received for the new request