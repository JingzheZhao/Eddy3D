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

## 2024-03-12 - [UTCI.Binning() performance improvement]
**Learning:** In net8.0, calling `Math.Round()` combined with `.GroupBy()` and `.ToDictionary()` inside an active loop tracking counts caused massive GC overhead for multi-category processing. LINQ allocations are notoriously expensive in tight mathematical loops over large lists (like 8760-hour arrays).
**Action:** Replace `GroupBy().ToDictionary()` mapping with a pre-allocated array map (e.g. `int[] counts`) when the bin range is known, transforming O(N) multi-pass allocation-heavy operations into strict zero-allocation single-pass loops.

## 2024-11-20 - Replace Math.Pow with direct multiplication in hot loops
**Learning:** Using `Math.Pow` or mathematical exponentiation repeatedly inside math-heavy simulation loops (like processing 8760 hours of annual weather data) creates significant overhead in C#.
**Action:** When finding powers in hot loops, replace `Math.Pow(X, y)` with direct multiplications precalculated into local variables (`double X2 = X * X`) and avoid powers of 10 (`Math.Pow(10, -10)`) by replacing them with E-notation literal constants (`1E-10`). This simple change drastically reduces calculation overhead.

## 2024-03-16 - Math.Pow Redundancies
**Learning:** The codebase contains redundant recursive math power operations like `Math.Pow(Math.Pow(x, 0.25), 4)` which calculate roots only to immediately raise them to the power again, causing extreme and unnecessary Math overhead.
**Action:** When inspecting mathematical models (like UTCI, MRT, PET), check for algebraically cancellable operations (e.g. `(x^0.25)^4 = x`) and explicitly define unboxed power variables to avoid `Math.Pow` altogether.

## 2024-11-20 - Math.Pow overhead in PET inner loop
**Learning:** Using `Math.Pow` repeatedly with the same variables within the tight `PET` calculation loop introduces a measurable performance penalty. The original code used repetitive mathematical operations such as calculating `Math.Pow(mbody, 0.75)` and repeatedly multiplying large tuples together.
**Action:** Precalculate `Math.Pow` variables such as fractional exponents, and precalculate repeated inner-loop sequence multiplications rather than performing identical multiplications multiple times. This can yield performance improvements with minimal structural changes.

## 2025-03-22 - Array allocations in high-frequency root-finding loops
**Learning:** Python-to-C# translations often carry over redundant code like allocating a new array from an existing array (e.g. `var arr = new double[3] { T[0], T[1], T[2] }` where `T` is already a `double[]`), and creating arrays for vectors that only hold 3 values where simple local variables will suffice. When nested inside non-linear root finding loops (`Broyden.FindRoot`) which itself is nested in parallel array processing, these allocate immense numbers of short-lived objects on the heap, thrashing the GC.
**Action:** Always scan inner calculation loops for `new []` allocations and replace them with local variables (e.g., `double enbal0`, `double enbal1`, `double enbal2`) or `ref` struct patterns to bypass GC overhead in math-heavy `.cs` files.

## 2025-05-18 - Replacing Tuples with ValueTuples inside tight loops
**Learning:** Using `Tuple<T1, T2>` (a reference type) inside a high-frequency loop like the non-linear root finding solver `Broyden.FindRoot` forces unnecessary and massive heap allocations on every single iteration.
**Action:** Replace `Tuple` with `ValueTuple` (e.g., `(T1, T2)`) in high-frequency methods, especially when they are called from inner loops or objective functions passed to solvers. This eliminates memory allocation overhead and reduces GC pressure. Note that this changes the method signature, but it remains source-compatible if implicit typing (`var`) is used.
## 2026-03-30 - [Optimize High-Frequency Method Calls and Array Lookups in Hot Loops]
**Learning:** Repeatedly accessing arrays or invoking numeric inspection methods (like `double.IsNaN`) inside nested O(N*8760) simulation loops significantly degrades performance due to bounds checks, memory indirection, and method call overhead.
**Action:** Always lift invariant array lookups out of inner loops. Replace expensive framework method calls for NaN and Infinity checks with implicit floating-point logic (e.g., `value > 0.0 && value < double.PositiveInfinity`) when filtering positive datasets in tight loops.
## 2026-04-01 - [Replace Math.Pow(x, 0.25) with Math.Sqrt(Math.Sqrt(x))]
**Learning:** In performance-critical C# mathematical loops, `Math.Pow(x, 0.25)` relies on generic software algorithms for floating point exponentiation which can be quite slow.
**Action:** Replace `Math.Pow(x, 0.25)` with `Math.Sqrt(Math.Sqrt(x))` to leverage fast hardware intrinsics instead of the slower, general-purpose floating-point software routines used by `Math.Pow`.
## 2025-05-18 - Avoid Math.Log inside O(N*8760) loops
**Learning:** High-frequency 8760-hour loops inside `Parallel.For` over probes (e.g., `UTCI` calculation) were recalculating static wind profile multipliers on every iteration involving expensive `Math.Log()` calls, totaling over 17 million redundant calculations per 1000 probes.
**Action:** When iterating over hours for a specific static probe, always hoist invariant property calculations out of the inner loop into the probe scope to eliminate massive mathematical overhead.
## 2024-05-24 - Faster Squaring without Math.Pow
**Learning:** `Math.Pow(x, 2)` introduces significant overhead in tight loops in C# (.NET) compared to a simple explicit multiplication (`x * x`). Given how many times `Get_fp_cylinder` can be called over 8760 hours of a year multiplied by number of probes, eliminating `Math.Pow` leads to a measurable performance increase.
**Action:** Always replace `Math.Pow(x, 2)` with direct multiplication `x * x` for numeric types where performance is critical.
