## 2025-05-14 - Initial Journal
**Learning:** Found that `ReturnMsg.PointsOutsideDomain` uses literal `\n` in a verbatim string and has a typo in the parameter name ('Indeces'). It also leaves a trailing comma in the list of indices.
**Action:** Fix the typo (while maintaining internal compatibility if needed), use proper newlines, and remove the trailing comma for better readability in Grasshopper remarks.
