using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace RhinoPlugin.Test.Xunit
{
    /// <summary>
    /// Initializes Rhino in-process once for the xUnit collection.
    /// </summary>
    public class XunitTestInitFixture : IDisposable
    {
        private static readonly object InitLock = new object();
        private static bool initialized;
        private static string rhinoSystemDir;
        private static string rhinoManagedPlugInsDir;
        private static string rhinoFrameworksDir;
        private static string grasshopperPath;
        private static bool rhinoInProcessStarted;

        /// <summary>Whether the Rhino in-process host was successfully started.</summary>
        public static bool RhinoAvailable { get; private set; }

        /// <summary>Human-readable reason why Rhino hosting is unavailable (null when available).</summary>
        public static string RhinoSkipReason { get; private set; }

        public XunitTestInitFixture()
        {
            lock (InitLock)
            {
                if (initialized)
                {
                    return;
                }

                initialized = true;

                if (!Environment.Is64BitProcess)
                {
                    RhinoAvailable = false;
                    RhinoSkipReason = "Tests must be run as a 64-bit process.";
                    if (TestExecutionPolicy.ShouldFailFastOnRhinoHostInitialization())
                    {
                        throw new InvalidOperationException(RhinoSkipReason);
                    }

                    return;
                }

                var nativeHostSkipReason = TestExecutionPolicy.GetSkipReason(TestExecutionRequirement.RhinoNativeHost);
                var rhinoInstallSkipReason = TestExecutionPolicy.GetSkipReason(TestExecutionRequirement.RhinoInstalled);

                try
                {
                    // On macOS, tests that only require Rhino installation may still run.
                    // If Rhino is not installed, skip before probing installation paths.
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && rhinoInstallSkipReason != null)
                    {
                        RhinoAvailable = false;
                        RhinoSkipReason = rhinoInstallSkipReason;
                        Console.WriteLine($"[XunitTestInitFixture] Rhino install unavailable on macOS - {rhinoInstallSkipReason}");
                        return;
                    }

                    // On non-mac platforms where native host is skipped by policy (for example Windows Server),
                    // skip immediately without probing Rhino runtime paths.
                    if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && nativeHostSkipReason != null)
                    {
                        RhinoAvailable = false;
                        RhinoSkipReason = nativeHostSkipReason;
                        Console.WriteLine($"[XunitTestInitFixture] Rhino host skipped by policy - {nativeHostSkipReason}");
                        return;
                    }

                    LocateRhinoInstall();
                    ConfigureAssemblyResolution();

                    // On macOS we configure runtime assembly resolution, but native host is intentionally skipped.
                    if (nativeHostSkipReason != null)
                    {
                        RhinoAvailable = false;
                        RhinoSkipReason = nativeHostSkipReason;
                        Console.WriteLine($"[XunitTestInitFixture] Rhino host skipped by policy - {nativeHostSkipReason}");
                        return;
                    }

                    StartRhinoCore();
                    RhinoAvailable = true;
                    RhinoSkipReason = null;
                }
                catch (Exception ex)
                {
                    RhinoAvailable = false;
                    RhinoSkipReason = $"Rhino in-process host could not be started: {ex.Message}";
                    Console.WriteLine($"[XunitTestInitFixture] Rhino init failed: {ex}");

                    if (TestExecutionPolicy.ShouldFailFastOnRhinoHostInitialization())
                    {
                        throw new InvalidOperationException(RhinoSkipReason, ex);
                    }
                }
            }
        }

        private static void LocateRhinoInstall()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                var windowsCandidates = new[]
                {
                    Path.Combine(programFiles, "Rhino WIP", "System"),
                    Path.Combine(programFiles, "Rhino 8", "System")
                };

                foreach (var candidate in windowsCandidates)
                {
                    if (Directory.Exists(candidate))
                    {
                        rhinoSystemDir = candidate;
                        rhinoManagedPlugInsDir = candidate;
                        var rhinoBaseDir = Path.GetDirectoryName(candidate);
                        if (!string.IsNullOrEmpty(rhinoBaseDir))
                        {
                            var gh = Path.Combine(rhinoBaseDir, "Plug-ins", "Grasshopper", "Grasshopper.dll");
                            if (File.Exists(gh))
                            {
                                grasshopperPath = gh;
                            }
                        }
                        return;
                    }
                }

                throw new InvalidOperationException("Rhino install not found on Windows (expected Rhino WIP or Rhino 8).");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var macCandidates = new[]
                {
                    "/Applications/RhinoWIP.app/Contents/Frameworks/RhCore.framework/Versions/A/Resources",
                    "/Applications/Rhino 8.app/Contents/Frameworks/RhCore.framework/Versions/A/Resources",
                    "/Applications/Rhino.app/Contents/Frameworks/RhCore.framework/Versions/A/Resources"
                };

                foreach (var candidate in macCandidates)
                {
                    if (!Directory.Exists(candidate))
                    {
                        continue;
                    }

                    rhinoSystemDir = candidate;
                    rhinoManagedPlugInsDir = Path.Combine(candidate, "ManagedPlugIns");
                    rhinoFrameworksDir = Path.GetFullPath(Path.Combine(candidate, "..", "..", "..", ".."));

                    var gh = Path.Combine(rhinoManagedPlugInsDir, "GrasshopperPlugin.rhp", "Grasshopper.dll");
                    if (File.Exists(gh))
                    {
                        grasshopperPath = gh;
                    }

                    return;
                }

                throw new InvalidOperationException("Rhino install not found on macOS (expected Rhino 8/WIP app in /Applications).");
            }

            throw new PlatformNotSupportedException(TestExecutionPolicy.UnsupportedPlatformReason);
        }

        private static void ConfigureAssemblyResolution()
        {
            var existingPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(rhinoSystemDir) && !existingPath.Contains(rhinoSystemDir, StringComparison.OrdinalIgnoreCase))
            {
                Environment.SetEnvironmentVariable("PATH", existingPath + Path.PathSeparator + rhinoSystemDir);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && !string.IsNullOrWhiteSpace(rhinoFrameworksDir))
            {
                var existingFallback = Environment.GetEnvironmentVariable("DYLD_FALLBACK_LIBRARY_PATH") ?? string.Empty;
                if (!existingFallback.Contains(rhinoFrameworksDir, StringComparison.Ordinal))
                {
                    var value = string.IsNullOrWhiteSpace(existingFallback)
                        ? rhinoFrameworksDir
                        : existingFallback + Path.PathSeparator + rhinoFrameworksDir;
                    Environment.SetEnvironmentVariable("DYLD_FALLBACK_LIBRARY_PATH", value);
                }
            }

            AppDomain.CurrentDomain.AssemblyResolve += ResolveRhinoAssemblies;
        }

        private static Assembly ResolveRhinoAssemblies(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name).Name;
            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                return null;
            }

            var loadedAssembly = GetLoadedAssembly(assemblyName);
            if (loadedAssembly != null)
            {
                return loadedAssembly;
            }

            var probes = new[]
            {
                Path.Combine(AppContext.BaseDirectory, assemblyName + ".dll"),
                !string.IsNullOrWhiteSpace(grasshopperPath) ? Path.Combine(Path.GetDirectoryName(grasshopperPath) ?? string.Empty, assemblyName + ".dll") : null,
                !string.IsNullOrWhiteSpace(rhinoManagedPlugInsDir) ? Path.Combine(rhinoManagedPlugInsDir, assemblyName + ".dll") : null,
                !string.IsNullOrWhiteSpace(rhinoSystemDir) ? Path.Combine(rhinoSystemDir, assemblyName + ".dll") : null
            };

            foreach (var probe in probes)
            {
                if (!string.IsNullOrWhiteSpace(probe) && File.Exists(probe))
                {
                    try
                    {
                        return Assembly.LoadFrom(probe);
                    }
                    catch (FileLoadException)
                    {
                        var alreadyLoaded = GetLoadedAssembly(assemblyName);
                        if (alreadyLoaded != null)
                        {
                            return alreadyLoaded;
                        }
                    }
                }
            }

            return null;
        }

        private static void StartRhinoCore()
        {
            try
            {
                var result = LaunchInProcess(0, 0);
                if (result != 1)
                {
                    throw new InvalidOperationException($"LaunchInProcess returned unexpected result: {result}.");
                }

                rhinoInProcessStarted = true;
                Console.WriteLine("Rhino in-process host initialized.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to initialize Rhino in-process host.", ex);
            }
        }

        private static Assembly GetLoadedAssembly(string assemblySimpleName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (string.Equals(asm.GetName().Name, assemblySimpleName, StringComparison.OrdinalIgnoreCase))
                {
                    return asm;
                }
            }

            return null;
        }

        public void Dispose()
        {
            lock (InitLock)
            {
                if (rhinoInProcessStarted)
                {
                    try
                    {
                        ExitInProcess();
                    }
                    catch
                    {
                        // Best effort cleanup for unmanaged Rhino host.
                    }

                    rhinoInProcessStarted = false;
                }

                initialized = false;
            }
        }

        [DllImport("RhinoLibrary.dll", EntryPoint = "LaunchInProcess")]
        private static extern int LaunchInProcess(int reserved1, int reserved2);

        [DllImport("RhinoLibrary.dll", EntryPoint = "ExitInProcess")]
        private static extern int ExitInProcess();
    }

    [CollectionDefinition("Rhino Collection")]
    public class RhinoCollection : ICollectionFixture<XunitTestInitFixture>
    {
    }
}
