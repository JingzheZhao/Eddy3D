using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
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
        private static object rhinoCore;
        private static string rhinoSystemDir;
        private static string rhinoManagedPlugInsDir;
        private static string rhinoFrameworksDir;
        private static string grasshopperPath;
        private static bool nativeResolverConfigured;
        private static IntPtr rhinoNativeLibraryHandle;

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
                    return;
                }

                try
                {
                    LocateRhinoInstall();
                    ConfigureAssemblyResolution();

                    // The Rhino in-process host (LaunchInProcess P/Invoke) only works on Windows.
                    // On macOS, the native call triggers an unrecoverable NSException crash.
                    // We still configured assembly resolution above so RhinoCommon types can load.
                    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        RhinoAvailable = false;
                        RhinoSkipReason = "Rhino in-process hosting requires Windows (RhinoLibrary.dll P/Invoke).";
                        Console.WriteLine($"[XunitTestInitFixture] Non-Windows platform — Rhino host skipped. Assembly resolution configured.");
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
                    Console.WriteLine($"[XunitTestInitFixture] Rhino init failed (tests will skip): {ex}");
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

            throw new PlatformNotSupportedException("Rhino tests currently support Windows and macOS only.");
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
                var rhinoCommonPath = Path.Combine(rhinoSystemDir, "RhinoCommon.dll");
                if (!File.Exists(rhinoCommonPath))
                {
                    throw new FileNotFoundException($"RhinoCommon runtime not found at expected location: {rhinoCommonPath}");
                }

                // Prefer loading RhinoCommon by identity so we do not duplicate-load the same assembly.
                var rhinoCoreType = Type.GetType("Rhino.Runtime.InProcess.RhinoCore, RhinoCommon", throwOnError: false);
                var rhinoCommonAssembly = rhinoCoreType?.Assembly;
                if (rhinoCommonAssembly == null)
                {
                    rhinoCommonAssembly = GetLoadedAssembly("RhinoCommon") ?? AssemblyLoadContext.Default.LoadFromAssemblyPath(rhinoCommonPath);
                    rhinoCoreType = rhinoCommonAssembly.GetType("Rhino.Runtime.InProcess.RhinoCore", throwOnError: true);
                }

                ConfigureNativeResolver(rhinoCommonAssembly);
                EnsureRhinoNativeLibraryLoaded();

                var windowStyleType = rhinoCommonAssembly.GetType("Rhino.Runtime.InProcess.WindowStyle", throwOnError: true);
                var hiddenWindowStyle = Enum.Parse(windowStyleType, "Hidden");
                var args = new[] { "/NOSPLASH", "/NOTEMPLATE", "/SCHEMENAME=Default" };
                rhinoCore = Activator.CreateInstance(rhinoCoreType, new object[] { args, hiddenWindowStyle });
                Console.WriteLine("RhinoCore initialized.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to initialize RhinoCore in-process.", ex);
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

        private static void ConfigureNativeResolver(Assembly rhinoCommonAssembly)
        {
            if (nativeResolverConfigured || !RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return;
            }

            if (rhinoCommonAssembly == null || string.IsNullOrWhiteSpace(rhinoFrameworksDir))
            {
                return;
            }

            NativeLibrary.SetDllImportResolver(rhinoCommonAssembly, (libraryName, assembly, searchPath) =>
            {
                if (!string.Equals(libraryName, "RhinoLibrary", StringComparison.Ordinal))
                {
                    return IntPtr.Zero;
                }

                var candidates = new[]
                {
                    Path.Combine(rhinoFrameworksDir, "RhinoLibrary.framework", "Versions", "A", "RhinoLibrary"),
                    Path.Combine(rhinoFrameworksDir, "RhinoLibrary.framework", "RhinoLibrary")
                };

                foreach (var candidate in candidates)
                {
                    if (!File.Exists(candidate))
                    {
                        continue;
                    }

                    if (NativeLibrary.TryLoad(candidate, out var handle))
                    {
                        return handle;
                    }
                }

                return IntPtr.Zero;
            });

            nativeResolverConfigured = true;
        }

        private static void EnsureRhinoNativeLibraryLoaded()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || rhinoNativeLibraryHandle != IntPtr.Zero || string.IsNullOrWhiteSpace(rhinoFrameworksDir))
            {
                return;
            }

            var candidates = new[]
            {
                Path.Combine(rhinoFrameworksDir, "RhinoLibrary.framework", "Versions", "A", "RhinoLibrary"),
                Path.Combine(rhinoFrameworksDir, "RhinoLibrary.framework", "RhinoLibrary")
            };

            foreach (var candidate in candidates)
            {
                if (!File.Exists(candidate))
                {
                    continue;
                }

                if (NativeLibrary.TryLoad(candidate, out rhinoNativeLibraryHandle))
                {
                    return;
                }
            }
        }

        public void Dispose()
        {
            lock (InitLock)
            {
                if (rhinoCore != null)
                {
                    if (rhinoCore is IDisposable d)
                    {
                        d.Dispose();
                    }
                    else
                    {
                        rhinoCore.GetType().GetMethod("Dispose", Type.EmptyTypes)?.Invoke(rhinoCore, null);
                    }

                    rhinoCore = null;
                }

                if (rhinoNativeLibraryHandle != IntPtr.Zero)
                {
                    NativeLibrary.Free(rhinoNativeLibraryHandle);
                    rhinoNativeLibraryHandle = IntPtr.Zero;
                }

                initialized = false;
            }
        }
    }

    [CollectionDefinition("Rhino Collection")]
    public class RhinoCollection : ICollectionFixture<XunitTestInitFixture>
    {
    }
}
