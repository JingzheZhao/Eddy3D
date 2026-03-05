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
