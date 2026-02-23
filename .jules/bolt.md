## 2025-02-19 - Caching Geometric Constants & Streaming File IO
**Learning:** Large constant arrays in methods are re-allocated on every call, causing significant GC pressure. `File.ReadAllLines` loads entire files into memory, which is inefficient for large simulation results.
**Action:** Use `static readonly` for constant geometric data. Use `File.ReadLines` with `Span<char>` for memory-efficient text parsing.
