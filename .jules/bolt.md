## 2024-05-24 - Optimize StringBuilder Concatenation
**Learning:** Using the `+` operator inside `StringBuilder.Append()` negates the performance benefits of `StringBuilder` by causing an intermediate string allocation on every execution, which can add up significantly inside loops.
**Action:** Always refactor `sb.Append(a + b)` into `sb.Append(a).Append(b)` to avoid unnecessary allocations and GC pressure.

## 2025-01-24 - Static Caching of Lookup Tables
**Learning:** Initializing large lookup tables or range arrays within a method that is called frequently (e.g., inside a loop over 8,760 hours for thousands of sensors) causes massive unnecessary heap allocations and garbage collection pressure.
**Action:** Move constant data, such as interpolation tables and range definitions, into `static readonly` class-level fields to ensure they are allocated only once.

## 2026-03-11 - Fast Angular Distance and Lookup Caching
**Learning:** Vector-based trigonometry for calculating angular distances between directions is expensive due to multiple transcendental function calls (Sin, Cos, Atan2) and object allocations. When processing 8,760 hours of data with limited discrete input values (e.g., integer degrees), a simple lookup table is significantly faster than repeated searches.
**Action:** Use modular arithmetic for angular distance between circular values: `int diff = Math.Abs(dir1 - dir2) % 360; return diff > 180 ? 360 - diff : diff;`. For discrete inputs like wind directions (0-359), use a 360-element array cache to store results of expensive calculations or searches.

## 2024-05-25 - Avoid O(N*M) Dictionary Hash Lookups and Enum.ToString() String Allocations
**Learning:** Using `Enum.ToString()` and string dictionaries inside nested O(N*M) loops creates massive redundant string allocations and lookup overhead, causing extreme garbage collection pressure.
**Action:** Replace `Enum.ToString()` dictionary logic inside hot loops with an upfront mapping logic that indexes into simple arrays.

## 2024-05-26 - Outer Loop Parallelization and False Sharing
**Learning:** Parallelizing the inner loop of a nested O(Hours * Sensors) simulation with trivial per-iteration work causes excessive task scheduling overhead and can lead to false sharing when multiple threads write to adjacent memory in the same row.
**Action:** Parallelize the outer (temporal) loop instead of the inner (spatial) loop. This improves cache locality, allows hoisting temporal-invariant calculations (like solar projection factors), and ensures each thread writes to its own distinct row, eliminating false sharing.
