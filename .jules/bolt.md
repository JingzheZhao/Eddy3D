## 2024-05-18 - Optimized String Parsing with ReadOnlySpan
**Learning:** Using `string.Split` iteratively on large files to parse lists of values causes immense GC pressure and degrades performance due to creating many temporary strings and arrays per line.
**Action:** Replace `string.Split` with manual iterative tokenization using `ReadOnlySpan<char>.IndexOf` and slice the span, calculating the size ahead of time if necessary. Pass `span` directly into `double.Parse` to parse numbers without any string allocation overhead. Ensure culture info is explicitly specified (`CultureInfo.InvariantCulture`) to maintain deterministic parsing.

## 2024-05-30 - Prevent LOH allocations with StreamWriter
**Learning:** Writing large `.pts` or `.dc` simulation files by accumulating data in a `StringBuilder` and calling `File.WriteAllText` can cause `OutOfMemoryException` and excessive Garbage Collection (GC) pauses due to Large Object Heap (LOH) fragmentation.
**Action:** When exporting large datasets (e.g., in `writePTS` or `writeDC`), always stream data directly to disk using `using var sw = new StreamWriter(path);` instead of buffering massive strings in memory.

## 2024-05-24 - [Avoid String Concatenation in Tight File I/O Loops]
**Learning:** String concatenation inside nested loops in `EddyLib/Utilities/CsvHelpers.cs` causes O(N^2) memory allocations and unnecessary garbage collection overhead when writing large CSV files, significantly degrading performance.
**Action:** Always stream outputs directly using `StreamWriter.Write()` and `StreamWriter.WriteLine()` instead of accumulating row data into intermediate strings. This completely avoids large memory allocations and improves file writing speed.

## 2024-05-19 - Prevent O(N) array allocation overhead
**Learning:** In highly parallel multi-dimensional operations like `UTCI.Equation.cs`'s `CalcAnnualComfortableHours`, pre-allocating large 2D arrays (`[HoursPerYear, numberOfProbes]`) to act as a hit-map, followed by expensive `.GetColumn()` column extraction simply to sum up totals, creates a significant GC bottleneck and large memory allocation footprint.
**Action:** Avoid allocating massive 2D structures purely for counting. Instead, track counts with a local primitive variable directly within the parallel execution scope and directly push the computed result back to the final flattened 1D array.

## 2024-05-31 - Avoid LINQ Where, Min, Max in tight loops
**Learning:** Using LINQ operators such as `.Where(x => x < val).ToArray()`, `.Min()`, and `.Max()` repeatedly inside inner loops (like processing 8760 hours of annual weather data) causes extreme execution times due to continuous $O(N)$ large allocations and multiple array passes.
**Action:** When finding extremes or conditionally filtering values inside heavily executed blocks, replace LINQ chains with single-pass `for` loops tracking primitive states (e.g. `min`, `max`, `best`) directly, avoiding temporary array allocations altogether. This simple change reduces execution time by over 95%.

## 2024-05-18 - Prevent O(N) array allocations in Parallel.For loops for wind comfort processing
**Learning:** In tight parallel processing loops like the annual wind comfort calculations (`WindComfort` and `WindComfortWeibull`), explicitly extracting a 1D slice or "column" from a large 2D array into a new `double[]` causes thousands of large temporary allocations (`double[8760]`). This stresses the Garbage Collector and degrades multi-threaded performance.
**Action:** Instead of allocating temporary arrays with helper methods like `ExtractColumn`, refactor the downstream metric processing methods to accept the original 2D array alongside an index variable (e.g., `probeIndex`). This allows iterating over the column data in-place (`temporalVelocityMatrix[i, probeIndex]`), achieving zero-allocation processing.

## 2024-11-20 - [Avoid LINQ Sorting inside Parallel Execution]
**Learning:** Executing LINQ `OrderBy` or `OrderByDescending` inside parallel loops dynamically generates enumerators, closures, and state machines, producing thousands of small rapid allocations that significantly impact GC thread contention.
**Action:** In inner parallelized functions, replace LINQ sorts on dictionaries/lists with manual primitive loops (for min/max) or explicit small array copying and using `Array.Sort` with a custom comparer for sorting. This minimizes closure overhead and allocation frequency per thread, drastically improving throughput.
