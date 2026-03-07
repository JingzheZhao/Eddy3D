## 2024-05-18 - Optimized String Parsing with ReadOnlySpan
**Learning:** Using `string.Split` iteratively on large files to parse lists of values causes immense GC pressure and degrades performance due to creating many temporary strings and arrays per line.
**Action:** Replace `string.Split` with manual iterative tokenization using `ReadOnlySpan<char>.IndexOf` and slice the span, calculating the size ahead of time if necessary. Pass `span` directly into `double.Parse` to parse numbers without any string allocation overhead. Ensure culture info is explicitly specified (`CultureInfo.InvariantCulture`) to maintain deterministic parsing.

## 2024-05-18 - Optimized String Parsing with ReadOnlySpan
**Learning:** Using `string.Split` iteratively on large files to parse lists of values causes immense GC pressure and degrades performance due to creating many temporary strings and arrays per line.
**Action:** Replace `string.Split` with manual iterative tokenization using `ReadOnlySpan<char>.IndexOf` and slice the span, calculating the size ahead of time if necessary. Pass `span` directly into `double.Parse` to parse numbers without any string allocation overhead. Ensure culture info is explicitly specified (`CultureInfo.InvariantCulture`) to maintain deterministic parsing.
## 2024-05-30 - Prevent LOH allocations with StreamWriter
**Learning:** Writing large `.pts` or `.dc` simulation files by accumulating data in a `StringBuilder` and calling `File.WriteAllText` can cause `OutOfMemoryException` and excessive Garbage Collection (GC) pauses due to Large Object Heap (LOH) fragmentation.
**Action:** When exporting large datasets (e.g., in `writePTS` or `writeDC`), always stream data directly to disk using `using var sw = new StreamWriter(path);` instead of buffering massive strings in memory.

## 2024-05-24 - [Avoid String Concatenation in Tight File I/O Loops]
**Learning:** String concatenation inside nested loops in `EddyLib/Utilities/CsvHelpers.cs` causes O(N^2) memory allocations and unnecessary garbage collection overhead when writing large CSV files, significantly degrading performance.
**Action:** Always stream outputs directly using `StreamWriter.Write()` and `StreamWriter.WriteLine()` instead of accumulating row data into intermediate strings. This completely avoids large memory allocations and improves file writing speed.
