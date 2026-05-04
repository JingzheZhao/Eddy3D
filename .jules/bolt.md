## 2024-05-24 - Optimize StringBuilder Concatenation
**Learning:** Using the `+` operator inside `StringBuilder.Append()` negates the performance benefits of `StringBuilder` by causing an intermediate string allocation on every execution, which can add up significantly inside loops.
**Action:** Always refactor `sb.Append(a + b)` into `sb.Append(a).Append(b)` to avoid unnecessary allocations and GC pressure.

## 2025-01-24 - Static Caching of Lookup Tables
**Learning:** Initializing large lookup tables or range arrays within a method that is called frequently (e.g., inside a loop over 8,760 hours for thousands of sensors) causes massive unnecessary heap allocations and garbage collection pressure.
**Action:** Move constant data, such as interpolation tables and range definitions, into `static readonly` class-level fields to ensure they are allocated only once.
