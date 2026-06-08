## 2024-05-14 - ToolTips on dynamic status labels
**Learning:** In Eto.Forms, when enhancing UX, it's beneficial to explicitly initialize interactive and informational UI elements (like read-only Labels displaying dynamic data such as status or progress) with descriptive `ToolTip` properties to improve accessibility. However, it's critical to avoid adding redundant ToolTips to static text labels that already visibly describe their context (e.g., a label that already has the text "Elapsed: 00:00:00" doesn't need an "Elapsed time" tooltip), as this creates an accessibility anti-pattern.
**Action:** When adding ToolTips to progress dialogs or similar UI components, target dynamic data fields while omitting labels that contain built-in text prefixes.

## 2026-03-10 - Dynamic parameter labeling for modal components
**Learning:** For Grasshopper components that switch between different modes of operation (e.g., visualizing by 'Hour' vs. 'Sensor'), dynamically updating the metadata (Name, NickName, Description) of input and output parameters in response to mode changes significantly reduces user confusion. This should be implemented via a dedicated `UpdateLabels()` method called from the constructor, `Read()`, and UI event handlers.
**Action:** Identify components with modal behavior and implement `UpdateLabels()` to ensure parameter context matches the active UI state.

## 2024-05-17 - Custom ToolTips for non-standard component regions
**Learning:** For Grasshopper components with custom rendered UI elements (like mode-switching buttons added to the capsule), standard tooltips for parameters do not cover these extra regions. Overriding `IsTooltipRegion(PointF)` and `SetupTooltip(PointF, GH_TooltipDisplayEventArgs)` allows providing context-sensitive help for these custom interactive areas, significantly improving discoverability of cryptic mode icons.
**Action:** When adding custom interactive regions to `GH_ComponentAttributes`, always implement corresponding tooltip overrides to explain the functionality of those regions.

## 2024-05-18 - ToolTips for Custom Attributes Regions
**Learning:** In Grasshopper plugins, when adding custom interactive regions (like buttons) via `GH_ComponentAttributes`, the standard component tooltips do not automatically cover these custom areas. It is necessary to explicitly override `IsTooltipRegion(PointF)` and `SetupTooltip(PointF, GH_TooltipDisplayEventArgs)` to enable tooltips. It's also critical to ensure the target bounds evaluated in these overrides are inflated identically to the bounds used in mouse event handlers (e.g., `bounds.Inflate(2, 2)`) to provide a consistent interaction zone.
**Action:** When adding or maintaining custom interactive UI elements in component attributes, verify that tooltip overrides are implemented and that their bounding box logic matches the hit-test bounds.
