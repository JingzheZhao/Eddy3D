## 2024-05-18 - Secure System Explorer Invocation without Shell Evaluation
**Vulnerability:** Command injection when opening a folder or path on user's system using `explorer.exe`, `open` or `xdg-open` due to `UseShellExecute = true` combined with unsanitized arguments.
**Learning:** Even though opening a folder seems harmless, passing user-controlled or complex paths with shell evaluation can lead to command injection on Windows, macOS, and Linux if a path contains certain meta-characters. `UseShellExecute = true` relies on OS shell evaluation.
**Prevention:** Use `ProcessStartInfo` with `UseShellExecute = false` and pass the path to the system explorer executable as a single entry within `ArgumentList`. This prevents shell evaluation of the path arguments.

## 2025-04-08 - Secure OS Command Invocation
**Vulnerability:** Command injection when invoking OS-level commands (e.g., `cmd.exe`, `/bin/ln`, `/bin/rm`) due to string concatenation or interpolation of unsanitized paths in the `Arguments` property or `StandardInput`.
**Learning:** Shell metacharacters in paths (like `;`, `&`, `|`) can execute arbitrary commands if `UseShellExecute` is `false` but the target executable is a shell (like `cmd.exe`) or if the shell itself interprets unescaped variables.
**Prevention:** Always use `ProcessStartInfo.ArgumentList.Add()` to pass arguments. For internal shell commands (e.g. `MKLINK`, `rd`), pass `cmd.exe` as the `FileName` and add `/c`, the command, and its arguments as separate items in `ArgumentList` to bypass shell parsing vulnerabilities. Avoid passing string-formatted commands to `StandardInput`.

## 2025-05-18 - Secure Process Invocation without Shell Redirection
**Vulnerability:** Command injection when invoking tools that use `<` and `>` to pipe input/output in `cmd.exe /c` where file paths are user-controlled.
**Learning:** Shell redirection operators (`<`, `>`) cannot be passed via `ArgumentList` directly to the tool, and using `cmd.exe` allows command injection if the user includes `&` in their paths.
**Prevention:** Invoke the executable directly (e.g., `rtrace`) using `ArgumentList` for parameters, and programmatically replace shell redirection with stream copying in C# (e.g., `File.OpenRead().CopyToAsync(process.StandardInput.BaseStream)`).

## 2025-05-24 - Secure OS Command Invocation Using ArgumentList
**Vulnerability:** Command injection in `FluidX3DCaseRunner.cs` via unsanitized script paths concatenated into `ProcessStartInfo.Arguments`.
**Learning:** Even when wrapping script paths in quotes, concatenating them into shell interpreters like `cmd.exe /k` or directly passing them via `UseShellExecute = true` without argument separation allows shell metacharacters (`&`, `;`) in the path to be evaluated as secondary commands, risking Remote Code Execution.
**Prevention:** Instead of string formatting `Arguments`, use `ProcessStartInfo.ArgumentList` in combination with `UseShellExecute = false`. On Windows, avoid `cmd.exe` by invoking the script path directly as the `FileName` with `UseShellExecute = true`, which safely delegates parsing to the OS without explicit shell interpretation.

## 2024-04-28 - Command Injection in DockerRunner
**Vulnerability:** Command injection via string concatenation in `ProcessStartInfo.Arguments`.
**Learning:** Concatenating user inputs into a command string for execution can allow attackers to inject malicious OS commands, even with `UseShellExecute = false`.
**Prevention:** Use `ProcessStartInfo.ArgumentList` to securely pass arguments, preventing the host OS parser from executing injected shell metacharacters.

## 2024-05-25 - Secure Temporary File Creation
**Vulnerability:** Symlink/TOCTOU attack vulnerability when creating temporary scripts or config files. The vulnerable pattern uses predictable directories (e.g., `Path.Combine(Path.GetTempPath(), "MyApp")`) and writes using `File.WriteAllText`.
**Learning:** `File.WriteAllText` combined with predictable paths allows an attacker to pre-create the file or directory, redirecting the write operation to overwrite critical system files or allowing them to intercept executing logic.
**Prevention:** Generate highly unpredictable filenames (e.g., `Guid.NewGuid()`) directly inside `Path.GetTempPath()`. Instantiate these files using `FileStream` configured with `FileMode.CreateNew`, `FileAccess.Write`, and `FileShare.None`. This combination guarantees atomic creation and exclusive access, rejecting the operation if the file already exists.
