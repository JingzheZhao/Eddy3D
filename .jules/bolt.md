## 2024-05-18 - Optimized String Parsing with ReadOnlySpan
**Learning:** Using `string.Split` iteratively on large files to parse lists of values causes immense GC pressure and degrades performance due to creating many temporary strings and arrays per line.
**Action:** Replace `string.Split` with manual iterative tokenization using `ReadOnlySpan<char>.IndexOf` and slice the span, calculating the size ahead of time if necessary. Pass `span` directly into `double.Parse` to parse numbers without any string allocation overhead. Ensure culture info is explicitly specified (`CultureInfo.InvariantCulture`) to maintain deterministic parsing.

## 2024-05-18 - Optimized String Parsing with ReadOnlySpan
**Learning:** Using `string.Split` iteratively on large files to parse lists of values causes immense GC pressure and degrades performance due to creating many temporary strings and arrays per line.
**Action:** Replace `string.Split` with manual iterative tokenization using `ReadOnlySpan<char>.IndexOf` and slice the span, calculating the size ahead of time if necessary. Pass `span` directly into `double.Parse` to parse numbers without any string allocation overhead. Ensure culture info is explicitly specified (`CultureInfo.InvariantCulture`) to maintain deterministic parsing.
## 2024-05-30 - Prevent LOH allocations with StreamWriter
**Learning:** Writing large `.pts` or `.dc` simulation files by accumulating data in a `StringBuilder` and calling `File.WriteAllText` can cause `OutOfMemoryException` and excessive Garbage Collection (GC) pauses due to Large Object Heap (LOH) fragmentation.
**Action:** When exporting large datasets (e.g., in `writePTS` or `writeDC`), always stream data directly to disk using `using var sw = new StreamWriter(path);` instead of buffering massive strings in memory.
