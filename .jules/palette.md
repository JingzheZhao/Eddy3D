## 2025-05-14 - Initial Journal
**Learning:** Found that `ReturnMsg.PointsOutsideDomain` uses literal `\n` in a verbatim string and has a typo in the parameter name ('Indeces'). It also leaves a trailing comma in the list of indices.
**Action:** Fix the typo (while maintaining internal compatibility if needed), use proper newlines, and remove the trailing comma for better readability in Grasshopper remarks.
## 2024-05-13 - [Inflating Hitboxes Requires Syncing MouseMove and MouseDown]
**Learning:** Small interactive controls in Grasshopper (like toggles or arrows) can be difficult to hit precisely. Inflate their hit-test bounding boxes by a few pixels (e.g., `bounds.Inflate(2f, 2f)`) identically inside BOTH `RespondToMouseMove` and `RespondToMouseDown`. Failing to update the click handler creates a frustrating UX where the visual affordance (hand cursor) mismatches the actual clickable area.
**Action:** When expanding hitboxes for hover effects, always verify that the corresponding click handler uses the same expanded bounding box.

## 2025-05-15 - [Interactive Hover Feedback for Custom Component Buttons]
**Learning:** Custom buttons in Grasshopper components (rendered via `GH_Capsule`) feel "dead" without visual feedback. Tracking a hover state (e.g., via `_hoverIndex`) and passing it to the `GH_Capsule.Render` method's `highlighted` parameter significantly improves the perceived responsiveness of the UI.
**Action:** For all custom interactive areas in `GH_ComponentAttributes`, implement hover tracking in `RespondToMouseMove` and trigger `sender.Invalidate()` to provide immediate visual feedback.
