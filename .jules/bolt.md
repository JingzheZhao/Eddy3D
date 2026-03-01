## 2025-02-19 - Caching Geometric Constants & Streaming File IO
**Learning:** Large constant arrays in methods are re-allocated on every call, causing significant GC pressure. `File.ReadAllLines` loads entire files into memory, which is inefficient for large simulation results.
**Action:** Use `static readonly` for constant geometric data. Use `File.ReadLines` with `Span<char>` for memory-efficient text parsing.

## 2025-02-19 - Optimizing String Parsing Loops with ReadOnlySpan
**Learning:** `string.Split()` in an `O(N)` parsing loop (e.g. log file or residual output parsers) creates multiple string arrays and countless intermediary string allocations, significantly increasing GC pressure.
**Action:** Replace `string.Split` with an explicit `ReadOnlySpan<char>` manual parsing loop and `double.TryParse(span)`. This single change dropped parsing allocations by >80% (from 377KB to 64KB per 1000 lines).

## 2025-02-19 - Optimizations must strictly preserve behaviour (Exception handling / state mutation)
**Learning:** When refactoring parsing functions, eagerly modifying external state or silently swallowing parse errors (e.g. `double.TryParse`) introduces critical data corruption bugs (such as column shifting and un-synchronized parallel arrays).
**Action:** When replacing `string.Split()`, use a reusable buffer (e.g. `List<double>.Clear()`) to accumulate row data. Only flush the buffer to the final arrays once validation (e.g. `count >= 2`) is complete, and retain `double.Parse` to maintain loud failure behavior on corrupted inputs.
