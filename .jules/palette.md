## 2024-05-15 - [Add Escape key support to dialogs]
**Learning:** In Eto.Forms dialogs, you can map the Escape key to a cancel action by setting the `AbortButton` property to a reference of the cancel button.
**Action:** When building custom dialogs with Eto.Forms, assign the `AbortButton` property to improve accessibility via keyboard shortcuts.

## 2024-05-15 - [Add numeric percentage to progress bars]
**Learning:** Progress bars (like `Eto.Forms.Drawable`) are more intuitive when they display the numerical progress directly, allowing users to precisely track execution time. Drawing a text shadow ensures contrast and visibility against varying fill colors.
**Action:** When designing a custom progress bar using Eto.Drawing, draw the numeric percentage centered over the bar using `SystemFonts.Label()` and a simple shadow (black text at +1px offset under white text).
