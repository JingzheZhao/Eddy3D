## 2024-05-14 - ToolTips on dynamic status labels
**Learning:** In Eto.Forms, when enhancing UX, it's beneficial to explicitly initialize interactive and informational UI elements (like read-only Labels displaying dynamic data such as status or progress) with descriptive `ToolTip` properties to improve accessibility. However, it's critical to avoid adding redundant ToolTips to static text labels that already visibly describe their context (e.g., a label that already has the text "Elapsed: 00:00:00" doesn't need an "Elapsed time" tooltip), as this creates an accessibility anti-pattern.
**Action:** When adding ToolTips to progress dialogs or similar UI components, target dynamic data fields while omitting labels that contain built-in text prefixes.

## 2026-03-10 - Dynamic parameter labeling for modal components
**Learning:** For Grasshopper components that switch between different modes of operation (e.g., visualizing by 'Hour' vs. 'Sensor'), dynamically updating the metadata (Name, NickName, Description) of input and output parameters in response to mode changes significantly reduces user confusion. This should be implemented via a dedicated `UpdateLabels()` method called from the constructor, `Read()`, and UI event handlers.
**Action:** Identify components with modal behavior and implement `UpdateLabels()` to ensure parameter context matches the active UI state.

## 2024-05-17 - Custom ToolTips for non-standard component regions
**Learning:** For Grasshopper components with custom rendered UI elements (like mode-switching buttons added to the capsule), standard tooltips for parameters do not cover these extra regions. Overriding `IsTooltipRegion(PointF)` and `SetupTooltip(PointF, GH_TooltipDisplayEventArgs)` allows providing context-sensitive help for these custom interactive areas, significantly improving discoverability of cryptic mode icons.
**Action:** When adding custom interactive regions to `GH_ComponentAttributes`, always implement corresponding tooltip overrides to explain the functionality of those regions.

## 2026-03-10 - Undo support and cursor states for manual toggles
**Learning:** Manual toggle components (like Safety Toggle) that bypass standard parameter wiring must explicitly call `RecordUndoEvent` before state changes to remain consistent with Grasshopper's UX. Additionally, `RespondToMouseMove` overrides should always check `!Owner.Locked` before changing the cursor to a "Hand" to avoid misleading users when the component is interaction-locked.
**Action:** Always wrap state changes in manual interaction handlers with undo events and respect the `Locked` property in mouse move handlers.

## 2026-03-10 - Immediate Canvas and Viewport Feedback
**Learning:** For Grasshopper components that calculate or visualize orientation (like the Wind Compass), providing immediate feedback on the canvas via the `Message` property (e.g., displaying "North") and adding fixed orientation markers (like an "N" indicator) in the viewport preview significantly improves usability. It reduces the need for users to connect additional components (like Panels) just to understand the current state or orientation of the component.
**Action:** Always consider adding a summary `Message` to components that have an internal state or primary result, and include orientation markers for spatial visualization components.

## 2024-05-18 - On-face dropdowns for complex settings
**Learning:** For components with many categorical integer inputs (like Mesh Mode or Turbulence Model), the standard Grasshopper right-click menu is often undiscovered by users. Implementing `DropdownComponentAttributes` with visible arrow (▼) menus on the component capsule significantly improves discoverability and ease of use. It is crucial that these custom attributes explicitly check `Owner.Locked` in their interaction handlers (`RespondToMouseDown`, `RespondToMouseMove`) to prevent misleading cursor changes or menu interactions on locked components.
**Action:** Use `DropdownComponentAttributes` for components with hidden categorical options and ensure the attributes respect the component's `Locked` state.

## 2026-03-11 - Momentary push-buttons on component capsules
**Learning:** For components that trigger one-off actions (like starting a simulation), implementing momentary push-buttons on the component capsule using `GH_ToggleParam` and `ProbeRunButtonAttributes` provides a superior UX compared to standard boolean inputs. To prevent the component from entering a "missing data" warning state when no external wire is connected, these parameters must be explicitly set as `Optional = true` in `RegisterInputParams`.
**Action:** When implementing on-face toggle buttons, always ensure the corresponding `GH_ToggleParam` is marked as `Optional` to maintain a clean component state.

## 2026-03-11 - Immediate Canvas Feedback for Stress Categories
**Learning:** For components that categorize continuous data into discrete ratings (like UTCI stress levels), displaying the human-readable category name (e.g., "No thermal stress") directly in the `GH_Component.Message` property significantly improves the "at-a-glance" usability of the canvas. When dealing with lists of data, falling back to a count summary (e.g., "8760 values") maintains a consistent visual feedback pattern.
**Action:** Implement `Message` feedback for all classification components to surface internal results without requiring Panels.

## 2026-05-29 - Enhanced Canvas Feedback via Component Messages
**Learning:** Grasshopper's `GH_Component.Message` property is an effective, non-intrusive way to surface internal state (like active indices in inspector components), configuration choices (like interpolation modes), or primary results (like comfort percentages) directly on the canvas. This provides immediate context and reduces the need for users to connect temporary Panels for simple status checks.
**Action:** Consistently use `Message` to display concise, relevant metadata or summaries that improve the "at-a-glance" readability of the visual script.

## 2024-06-07 - Immediate Canvas Feedback for Engine Installation
**Learning:** For components that perform environment or dependency checks (like 'Install Engines'), displaying the status summary directly in the `GH_Component.Message` property (e.g., "All Installed" or "Missing 2 Tools") provides immediate, glanceable feedback on the Grasshopper canvas. This is more discoverable than requiring users to check runtime message balloons or the output log.
**Action:** Implement `Message` feedback for all setup and diagnostic components to surface environment health at a glance.
## 2026-03-12 - Inflated Hit-test Bounds for Toggles
**Learning:** Small interactive controls in Grasshopper (like toggles or arrows) can be difficult to hit precisely. Inflate their hit-test bounding boxes by a few pixels (e.g., `bounds.Inflate(2f, 2f)`) identically across all mouse interaction handlers (e.g., `RespondToMouseMove`, `RespondToMouseDown`, and `RespondToMouseDoubleClick`). Failing to update all relevant handlers creates a frustrating UX where the visual affordance mismatches the actual clickable area.
**Action:** When creating or maintaining custom interactive elements with `GH_ComponentAttributes`, verify that identical bounds inflation logic is consistently applied in all mouse, click, and tooltip handler overrides.
