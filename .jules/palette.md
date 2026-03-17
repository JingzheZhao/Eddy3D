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
**Action:** When designing empty states for custom canvas drawings, always center the text horizontally and vertically using `StringFormat`, use a lighter font color (e.g., `140, 140, 140`) to indicate a placeholder, and optionally apply an italicized font (`GH_FontServer.StandardItalic`). Ensure placeholder text is capitalized for a polished look.

## 2026-03-17 - [Make Log Dialogs Resizable]
**Learning:** When designing `Eto.Forms` dialogs (like progress dialogs or log viewers) that contain dynamically updating data like logs, using a fixed `ClientSize` restricts users from expanding the window to read long lines or more history.
**Action:** Ensure `Resizable = true` is set and use `MinimumSize` instead of a fixed `ClientSize` to allow users to scale the window for better readability.
