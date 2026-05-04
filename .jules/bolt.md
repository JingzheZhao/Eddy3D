## 2024-05-24 - Optimize StringBuilder Concatenation
**Learning:** Using the `+` operator inside `StringBuilder.Append()` negates the performance benefits of `StringBuilder` by causing an intermediate string allocation on every execution, which can add up significantly inside loops.
**Action:** Always refactor `sb.Append(a + b)` into `sb.Append(a).Append(b)` to avoid unnecessary allocations and GC pressure.
