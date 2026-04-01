## 2024-05-15 - [Add Escape key support to dialogs]
**Learning:** In Eto.Forms dialogs, you can map the Escape key to a cancel action by setting the `AbortButton` property to a reference of the cancel button.
**Action:** When building custom dialogs with Eto.Forms, assign the `AbortButton` property to improve accessibility via keyboard shortcuts.

## 2024-05-15 - [Add numeric percentage to progress bars]
**Learning:** Progress bars (like `Eto.Forms.Drawable`) are more intuitive when they display the numerical progress directly, allowing users to precisely track execution time. Drawing a text shadow ensures contrast and visibility against varying fill colors.
**Action:** When designing a custom progress bar using Eto.Drawing, draw the numeric percentage centered over the bar using `SystemFonts.Label()` and a simple shadow (black text at +1px offset under white text).

## 2024-05-15 - [Inline feedback for clipboard operations]
**Learning:** Using a blocking `MessageBox` to confirm a successful clipboard copy action disrupts the user's flow and forces an unnecessary interaction.
**Action:** Replace success dialogs for minor actions (like "Copy Log") with inline visual feedback. Temporarily change the button text (e.g., to "Copied!") and use `Task.Delay` with `SynchronizationContext.Post` to revert it after a short delay (e.g., 2 seconds).

## 2024-05-15 - [Add tooltips to UI controls]
**Learning:** Providing context for status indicators and spinners helps users understand background processes without cluttering the UI.
**Action:** When enhancing UX in Eto.Forms, ensure UI elements (including Buttons, ProgressBars, Labels, TextAreas, and Spinners) include descriptive `ToolTip` properties upon instantiation to improve accessibility and provide user guidance.

## 2024-05-15 - [Use SystemColors for UI Controls]
**Learning:** Hardcoding colors like `Colors.Gray` or `Colors.Blue` in custom Eto.Forms controls can clash with the user's system theme (e.g., light vs dark mode, or custom accent colors), resulting in poor contrast or inconsistent UI styling.
**Action:** Always prefer `SystemColors` (like `SystemColors.Control`, `SystemColors.Highlight`, `SystemColors.ControlBackground`) over static colors when building custom drawable UI controls. This ensures they naturally integrate with the OS theme.

## 2024-05-15 - [Add Enter key support to dialogs]
**Learning:** In Eto.Forms dialogs, you can map the Enter key to a default action by setting the `DefaultButton` property.
**Action:** When building custom dialogs with Eto.Forms, assign the `DefaultButton` property to improve accessibility via keyboard shortcuts.

## 2024-05-15 - [Add visual hover feedback to Grasshopper component buttons]
**Learning:** Grasshopper component buttons (e.g., custom attributes) often lack visual affordance on hover, leaving users uncertain if the region is interactive.
**Action:** When designing custom `GH_ComponentAttributes` that include interactive regions like buttons, override `RespondToMouseMove` to dynamically change the cursor to `GH_Hand` (`Grasshopper.Instances.CursorServer.AttachCursor(sender, "GH_Hand")`) when hovering over the clickable bounds.

## 2024-06-25 - Improve Cursor Feedback with Reflection
**Learning:** In .NET 8 cross-platform Grasshopper plugins (like Eddy), adding UI visual feedback (e.g. changing the cursor to a hand on hover via `RespondToMouseMove`) requires reflection to access `Grasshopper.Instances.CursorServer.AttachCursor` due to System.Windows.Forms dependency issues. Looking up the MethodInfo via reflection on every mouse movement pixel is highly inefficient and unidiomatic for a high-frequency event loop.
**Action:** Always extract the reflected `MethodInfo` into a cached static field within the ComponentAttributes constructor to avoid unnecessary GC allocations and execution overhead during hover events.

## 2025-03-14 - [Improve Empty States in Custom Grasshopper Canvas Drawings]
**Learning:** When creating custom Grasshopper component drawings (like live charts directly on the canvas using `Eto.Drawing` or `System.Drawing`), empty states (e.g., "no data") placed in the top-left corner with default small text appear broken or like debugging artifacts.
**Action:** When designing empty states for custom canvas drawings, always center the text horizontally and vertically using `StringFormat`, use a lighter font color (e.g., `140, 140, 140`) to indicate a placeholder, and optionally apply an italicized font (`GH_FontServer.StandardItalic`). Avoid using all-caps text for these states, as it negatively impacts readability and accessibility.

## 2026-03-17 - [Make Log Dialogs Resizable]
**Learning:** When designing `Eto.Forms` dialogs (like progress dialogs or log viewers) that contain dynamically updating data like logs, using a fixed `ClientSize` restricts users from expanding the window to read long lines or more history.
**Action:** Ensure `Resizable = true` is set and use `MinimumSize` instead of a fixed `ClientSize` to allow users to scale the window for better readability.

## 2025-06-12 - [Append numeric percentage to window title in progress dialogs]
**Learning:** Progress bars in Eto.Forms dialogs only show progress when the window is visible. For long-running simulations, users often minimize the window or switch applications, losing visibility into the progress.
**Action:** When implementing progress dialogs in Eto.Forms (e.g., `ProgressDialog`), dynamically append the numeric progress percentage to the window `Title` to allow users to monitor long-running tasks from the OS taskbar or window switcher even when the application is minimized.

## 2025-08-01 - [Surface Keyboard Shortcuts in Tooltips]
**Learning:** While mapping keyboard keys (like Esc and Enter) to default dialog actions (`AbortButton`, `DefaultButton`) improves accessibility, users have no visual way to discover these shortcuts.
**Action:** When mapping UI elements to keyboard shortcuts, explicitly append the keyboard shortcut hint (e.g., "(Esc)", "(Enter)") to the element's `ToolTip` to improve discoverability.

## 2025-08-01 - [Initialize Dynamic Titles]
**Learning:** If a dialog's title dynamically updates to include a percentage (e.g., "Simulation Progress - 50%") via property setters or tick events, the initial title set in the constructor (e.g., "Simulation Progress") creates an inconsistent visual state before the first tick fires, lacking the expected numerical format.
**Action:** When implementing progress dialogs whose Title dynamically updates with a percentage, initialize the Title string in the constructor to include " - 0%" to provide immediate visual feedback before the first progress event fires.
## 2025-08-01 - [Add Initial State to Status Labels]
**Learning:** Instantiating UI elements like Eto.Forms `Label` components without an initial `Text` value (e.g., in a `ProgressDialog` status display) leaves the UI looking blank or unresponsive before the first progress event is received.
**Action:** When designing dynamic status labels, always initialize them with a default empty state `Text` (e.g., "Starting simulation...") to provide immediate visual feedback.

## 2026-03-24 - [Inline async loading states in Grasshopper]
**Learning:** For asynchronous operations triggered within Grasshopper components (e.g., downloading templates or files), avoiding blocking `MessageBox` dialogs or silent background tasks significantly improves UX. Users often wait idly without knowing an action is processing.
**Action:** Provide inline visual feedback by updating the component's `Message` property (e.g., `this.Message = "Downloading...";`) and explicitly forcing a canvas redraw via `Grasshopper.Instances.ActiveCanvas?.Refresh();`. Clear the message gracefully after the async action completes or fails to prevent stale UI states.

## 2024-05-18 - Non-blocking Dialogs for Async Operations
**Learning:** For asynchronous operations triggered within Grasshopper components (e.g., downloading templates or files), using a blocking `MessageBox` for errors creates a poor, frustrating UX by stealing focus and blocking the Grasshopper canvas interaction.
**Action:** Replace blocking error dialogs with inline visual feedback. Update the component's internal state (e.g., `this.Message = "Failed";`) and surface detailed errors via the built-in `AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ...)` method to provide non-disruptive, contextual feedback directly on the canvas element.
## 2024-03-29 - Wrap long text in Progress Dialogs
**Learning:** Eto.Forms `Label` components do not wrap text by default. In progress dialogs, where status messages can contain very long file paths or detailed error messages, this causes the text to be truncated or push the dialog bounds out of screen, rendering it inaccessible and hard to read.
**Action:** When initializing `Label` components in Eto.Forms intended for dynamic or potentially lengthy text updates (like logs or status readouts), explicitly set `Wrap = WrapMode.Word` to ensure the text remains readable within the container limits.
