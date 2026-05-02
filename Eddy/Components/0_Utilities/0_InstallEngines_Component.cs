using Eddy.Properties;
using EddyLib;
using EddyLib.Docker;
using EddyLib.FluidX3D;
using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace Eddy
{
    public class InstallEngines_Component : GH_Component
    {
        private static readonly bool IsMac = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        private const string EngineNameOpenFoamDocker = "OpenFOAM (Docker)";
        private const string EngineNameOpenFoamBlueCfd = "OpenFOAM (BlueCFD)";
        private const string EngineNameFluidX3D = "FluidX3D";

        public InstallEngines_Component()
          : base("Install Engines", "Install",
                IsMac
                    ? "Downloads and installs required simulation engines (EnergyPlus v9.4.0, Radiance, OpenFOAM (Docker), & FluidX3D source)."
                    : "Downloads and installs required simulation engines (EnergyPlus v9.4.0, Radiance, OpenFOAM (BlueCFD), & FluidX3D source).",
              EddyVersion.Name, "0 | Utilities")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Install EnergyPlus", "EP", "Set to True to download and launch EnergyPlus v9.4.0 installer.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Install Radiance", "Rad", "Set to True to download and install Radiance.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter(
                IsMac ? "Install OpenFOAM (Docker)" : "Install OpenFOAM (BlueCFD)",
                "CFD",
                IsMac ? "Set to True to open Docker Desktop download page for OpenFOAM (Docker)."
                      : "Set to True to download and launch blueCFD-Core 2020-1 installer for OpenFOAM (BlueCFD).",
                GH_ParamAccess.item, false);
            pManager.AddBooleanParameter(
                "Install FluidX3D",
                "FX3D",
                IsMac
                    ? "Set to True to clone/update FluidX3D source into the Eddy engines folder and pin to EDDY_FLUIDX3D_COMMIT (or Eddy default pin)."
                    : "Set to True to clone/update FluidX3D source into the Eddy engines folder, pin to EDDY_FLUIDX3D_COMMIT (or Eddy default pin), and launch the Visual Studio C++ Build Tools bootstrapper if required.",
                GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Log", "L", "Installation log.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool installEP = false;
            bool installRad = false;
            bool installCfd = false;
            bool installFluidX3D = false;

            if (!DA.GetData(0, ref installEP)) return;
            if (!DA.GetData(1, ref installRad)) return;
            if (!DA.GetData(2, ref installCfd)) return;
            if (!DA.GetData(3, ref installFluidX3D)) return;

            this.ClearRuntimeMessages();

            var missing = new System.Collections.Generic.List<string>();
            string log = "";

            string platform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" : "macOS";
            var snapshot = EngineInstallStatusCache.CreateSnapshot("Eddy3D", platform);

            try { DefaultDirectoriesAndPaths.CheckRadiance(); }
            catch { missing.Add("Radiance"); }

            try { DefaultDirectoriesAndPaths.CheckEnergyPlus(); }
            catch { missing.Add("EnergyPlus"); }

            bool dockerInstalled = false;
            string dockerDetails;
            try
            {
                DefaultDirectoriesAndPaths.CheckDocker();
                dockerInstalled = true;
                dockerDetails = EngineNameOpenFoamDocker + " is installed and running.";
            }
            catch (Exception ex)
            {
                dockerDetails = ex.Message;
                if (IsMac)
                {
                    missing.Add(EngineNameOpenFoamDocker);
                }
            }

            bool blueCfdInstalled = false;
            string blueCfdDetails;
            if (IsMac)
            {
                blueCfdDetails = EngineNameOpenFoamBlueCfd + " is not supported on macOS.";
            }
            else
            {
                try
                {
                    DefaultDirectoriesAndPaths.CheckBlueCfd();
                    blueCfdInstalled = true;
                    blueCfdDetails = EngineNameOpenFoamBlueCfd + " is installed.";
                }
                catch (Exception ex)
                {
                    blueCfdDetails = ex.Message;
                    missing.Add(EngineNameOpenFoamBlueCfd);
                }
            }

            bool fluidX3DInstalled = TryResolveInstalledFluidX3D(out _, out string fluidX3DDetails);
            if (!fluidX3DInstalled)
            {
                missing.Add(EngineNameFluidX3D);
            }

            bool windowsFluidX3DToolchainInstalled = true;
            string windowsFluidX3DToolchainDetails = "Visual Studio build tools are not required on macOS.";
            if (!IsMac)
            {
                windowsFluidX3DToolchainInstalled =
                    TryResolveWindowsFluidX3DBuildTools(out windowsFluidX3DToolchainDetails);
                if (!windowsFluidX3DToolchainInstalled)
                {
                    missing.Add("Visual Studio C++ Build Tools (FluidX3D)");
                }
            }

            EngineInstallStatusCache.SetEngineStatus(snapshot, EngineNameOpenFoamDocker, dockerInstalled, dockerDetails);
            EngineInstallStatusCache.SetEngineStatus(snapshot, EngineNameOpenFoamBlueCfd, blueCfdInstalled, blueCfdDetails);
            EngineInstallStatusCache.SetEngineStatus(snapshot, EngineNameFluidX3D, fluidX3DInstalled, fluidX3DDetails);
            // Legacy keys kept for compatibility with existing cached lookups in older versions.
            EngineInstallStatusCache.SetEngineStatus(snapshot, "Docker", dockerInstalled, dockerDetails);
            EngineInstallStatusCache.SetEngineStatus(snapshot, "BlueCFD", blueCfdInstalled, blueCfdDetails);
            EngineInstallStatusCache.SetEngineStatus(snapshot, "FluidX3D", fluidX3DInstalled, fluidX3DDetails);
            EngineInstallStatusCache.SetEngineStatus(snapshot, "VisualStudioBuildTools", windowsFluidX3DToolchainInstalled, windowsFluidX3DToolchainDetails);
            EngineInstallStatusCache.SetEngineStatus(snapshot, "WSL", false, "WSL is not used by Eddy3D.");

            if (!EngineInstallStatusCache.TryWrite(EngineInstallStatusCache.EddyCachePath, snapshot, out string cacheWriteError))
            {
                string cacheWarning = "Install-check cache could not be written: " + cacheWriteError;
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, cacheWarning);
                log += cacheWarning + "\n";
            }

            if (!IsMac && !windowsFluidX3DToolchainInstalled)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, windowsFluidX3DToolchainDetails);
                log += windowsFluidX3DToolchainDetails + "\n";
            }

            if (missing.Count > 0)
            {
                string missingStr = "The following tools are missing: " + string.Join(", ", missing);
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, missingStr);
                log += missingStr + "\n";
            }
            else
            {
                string presentStr = "All simulation tools are correctly installed.";
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, presentStr);
                log += presentStr + "\n";
            }

            if (installEP)
            {
                log += InstallEnergyPlus();
            }

            if (installRad)
            {
                log += InstallRadiance();
            }

            if (installCfd)
            {
                log += IsMac ? InstallDocker() : InstallBlueCfd();
            }

            if (installFluidX3D)
            {
                log += InstallFluidX3D();
            }

            DA.SetData(0, log);
        }

        private string InstallEnergyPlus()
        {
            string url = IsMac
                ? "https://github.com/NREL/EnergyPlus/releases/download/v9.4.0/EnergyPlus-9.4.0-998c4b761e-Darwin-macOS10.15-x86_64.dmg"
                : "https://github.com/NREL/EnergyPlus/releases/download/v9.4.0/EnergyPlus-9.4.0-998c4b761e-Windows-x86_64.exe";
            string ext = IsMac ? ".dmg" : ".exe";
            string tempFile = Path.Combine(Path.GetTempPath(), "EnergyPlus-9.4.0-Installer" + ext);

            try
            {
                using (var client = new WebClient())
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    client.DownloadFile(url, tempFile);
                }

                if (File.Exists(tempFile))
                {
                    if (IsMac)
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "open",
                            UseShellExecute = false
                        };
                        psi.ArgumentList.Add(tempFile);
                        Process.Start(psi);
                    }
                    else
                    {
                        Process.Start(tempFile);
                    }
                    return string.Format("EnergyPlus installer launched from {0}. Please complete the installation manually.\n", tempFile);
                }
                else
                {
                    return "Error: EnergyPlus installer download failed.\n";
                }
            }
            catch (Exception ex)
            {
                return string.Format("Error installing EnergyPlus: {0}\n", ex.Message);
            }
        }

        private string InstallRadiance()
        {
            if (IsMac)
            {
                return InstallRadianceMacOS();
            }

            string archiveName = DefaultDirectoriesAndPaths.RadianceWindowsArchiveName;
            string url = GetRadianceReleaseUrl(archiveName);
            string zipFile = Path.Combine(Path.GetTempPath(), archiveName);

            string baseDir = DefaultDirectoriesAndPaths.Eddy3DInstallDir;
            string targetDir = Path.Combine(baseDir, DefaultDirectoriesAndPaths.RadianceWindowsFolderName);

            try
            {
                if (!Directory.Exists(baseDir)) Directory.CreateDirectory(baseDir);

                if (Directory.Exists(targetDir))
                {
                    Directory.Delete(targetDir, true);
                }
                Directory.CreateDirectory(targetDir);

                using (var client = new WebClient())
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    client.DownloadFile(url, zipFile);
                }

                ZipFile.ExtractToDirectory(zipFile, targetDir);

                return string.Format("Radiance installed successfully to {0}.\n", targetDir);
            }
            catch (Exception ex)
            {
                return string.Format("Error installing Radiance: {0}\n", ex.Message);
            }
        }

        private string InstallRadianceMacOS()
        {
            bool isArm64 = RuntimeInformation.ProcessArchitecture == Architecture.Arm64;
            string archiveName = isArm64
                ? DefaultDirectoriesAndPaths.RadianceMacOSArm64ArchiveName
                : DefaultDirectoriesAndPaths.RadianceMacOSArchiveName;
            string url = GetRadianceReleaseUrl(archiveName);
            string zipFile = Path.Combine(Path.GetTempPath(), archiveName);

            string baseDir = DefaultDirectoriesAndPaths.Eddy3DInstallDir;
            string targetDir = Path.Combine(
                baseDir,
                isArm64
                    ? DefaultDirectoriesAndPaths.RadianceMacOSArm64FolderName
                    : DefaultDirectoriesAndPaths.RadianceMacOSFolderName);

            try
            {
                if (!Directory.Exists(baseDir)) Directory.CreateDirectory(baseDir);

                if (Directory.Exists(targetDir))
                {
                    Directory.Delete(targetDir, true);
                }
                Directory.CreateDirectory(targetDir);

                using (var client = new WebClient())
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    client.DownloadFile(url, zipFile);
                }

                ZipFile.ExtractToDirectory(zipFile, targetDir);

                // Set executable permissions on all files in bin/
                string binDir = Path.Combine(targetDir, "radiance", "bin");
                if (Directory.Exists(binDir))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "chmod",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    psi.ArgumentList.Add("-R");
                    psi.ArgumentList.Add("+x");
                    psi.ArgumentList.Add(binDir);
                    Process.Start(psi)?.WaitForExit(10000);
                }

                // Clean up zip
                try { File.Delete(zipFile); } catch { }

                return string.Format("Radiance installed successfully to {0}.\n", Path.Combine(targetDir, "radiance"));
            }
            catch (Exception ex)
            {
                return string.Format("Error installing Radiance: {0}\n", ex.Message);
            }
        }

        private static string GetRadianceReleaseUrl(string archiveName)
        {
            return $"https://github.com/LBNL-ETA/Radiance/releases/download/{DefaultDirectoriesAndPaths.RadianceReleaseTag}/{archiveName}";
        }

        private string InstallDocker()
        {
            try
            {
                if (DockerEnvironment.IsDockerAvailable())
                {
                    return EngineNameOpenFoamDocker + " is already installed and running.\n";
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = "https://www.docker.com/products/docker-desktop/",
                    UseShellExecute = true
                });

                return "Opened Docker Desktop download page for " + EngineNameOpenFoamDocker + ". Please install Docker Desktop and start it.\n";
            }
            catch (Exception ex)
            {
                return string.Format(
                    "Error: {0}\nPlease install Docker Desktop for {1} from https://www.docker.com/products/docker-desktop/\n",
                    ex.Message,
                    EngineNameOpenFoamDocker);
            }
        }

        private string InstallBlueCfd()
        {
            string url = "https://github.com/blueCFD/Core/releases/download/blueCFD-Core-2020-1/blueCFD-Core-2020-1-win64-setup.exe";
            string tempFile = Path.Combine(Path.GetTempPath(), "blueCFD-Core-2020-1-Installer.exe");

            try
            {
                using (var client = new WebClient())
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    client.DownloadFile(url, tempFile);
                }

                if (File.Exists(tempFile))
                {
                    Process.Start(tempFile);
                    return string.Format(
                        "blueCFD-Core installer launched for {0} from {1}. Please complete the installation manually.\n",
                        EngineNameOpenFoamBlueCfd,
                        tempFile);
                }
                else
                {
                    return "Error: blueCFD-Core installer download failed for " + EngineNameOpenFoamBlueCfd + ".\n";
                }
            }
            catch (Exception ex)
            {
                return string.Format("Error installing {0}: {1}\n", EngineNameOpenFoamBlueCfd, ex.Message);
            }
        }

        private static bool TryResolveInstalledFluidX3D(out string sourceRoot, out string details)
        {
            sourceRoot = null;

            var candidates = new List<string>();
            string fromEnv = Environment.GetEnvironmentVariable("EDDY_FLUIDX3D_SOURCE");
            if (!string.IsNullOrWhiteSpace(fromEnv))
            {
                candidates.Add(Path.GetFullPath(fromEnv.Trim()));
            }

            candidates.Add(Path.GetFullPath(FluidX3DAblWorkflow.GetDefaultSourceDirectory()));

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string candidate in candidates)
            {
                if (string.IsNullOrWhiteSpace(candidate) || !seen.Add(candidate))
                {
                    continue;
                }

                if (Directory.Exists(candidate) && FluidX3DAblWorkflow.IsValidSourceDirectory(candidate))
                {
                    sourceRoot = candidate;
                    if (TryResolveGitHeadCommit(candidate, out string commit))
                    {
                        details = "FluidX3D source found at: " + candidate + " (HEAD " + commit + ").";
                    }
                    else
                    {
                        details = "FluidX3D source found at: " + candidate + ".";
                    }
                    return true;
                }
            }

            details = "FluidX3D source not found. Expected at " + FluidX3DAblWorkflow.GetDefaultSourceDirectory()
                + " (or set EDDY_FLUIDX3D_SOURCE).";
            return false;
        }

        private static bool TryResolveWindowsFluidX3DBuildTools(out string details)
        {
            if (IsMac)
            {
                details = "Visual Studio build tools are not required on macOS.";
                return true;
            }

            IReadOnlyList<string> installedToolsets = FluidX3DAblWorkflow.GetInstalledWindowsPlatformToolsets();
            if (installedToolsets.Count > 0)
            {
                bool hasV142 = installedToolsets.Any(t => string.Equals(t, "v142", StringComparison.OrdinalIgnoreCase));
                string installedText = string.Join(", ", installedToolsets);
                details = hasV142
                    ? "Visual Studio C++ build tools detected for FluidX3D. Installed platform toolsets: " + installedText + "."
                    : "Visual Studio C++ build tools detected for FluidX3D. Installed platform toolsets: " + installedText
                        + ". Eddy will use the newest available toolset automatically.";
                return true;
            }

            details = "FluidX3D on Windows requires Visual Studio C++ build tools. Download: "
                + FluidX3DAblWorkflow.WindowsBuildToolsDownloadUrl
                + " | Workload: " + FluidX3DAblWorkflow.WindowsCppWorkloadId
                + " | Component: " + FluidX3DAblWorkflow.WindowsV142ToolsetComponentId + ".";
            return false;
        }

        private static string InstallFluidX3D()
        {
            string targetRoot = Path.GetFullPath(FluidX3DAblWorkflow.GetDefaultSourceDirectory());
            string pinnedCommit = FluidX3DAblWorkflow.ResolvePinnedCommit();
            StringBuilder log = new StringBuilder();

            if (!IsMac && !TryResolveWindowsFluidX3DBuildTools(out _))
            {
                log.Append(InstallWindowsFluidX3DBuildTools());
            }

            try
            {
                if (Directory.Exists(targetRoot)
                    && FluidX3DAblWorkflow.IsValidSourceDirectory(targetRoot)
                    && TryResolveGitHeadCommit(targetRoot, out string existingHead)
                    && existingHead.StartsWith(pinnedCommit, StringComparison.OrdinalIgnoreCase))
                {
                    log.Append("FluidX3D source already installed at pinned commit " + existingHead + ".\n");
                    return log.ToString();
                }

                FluidX3DAblWorkflow.EnsureSourceRepository(targetRoot, true, out string status, pinnedCommit);
                log.Append("FluidX3D source prepared at " + targetRoot + ".\n");
                log.Append("Pinned commit: " + pinnedCommit + "\n");
                if (!string.IsNullOrWhiteSpace(status))
                {
                    log.Append(status.Trim()).Append("\n");
                }

                return log.ToString();
            }
            catch (Exception ex)
            {
                log.Append("Error installing FluidX3D: " + ex.Message + "\n");
                return log.ToString();
            }
        }

        private static string InstallWindowsFluidX3DBuildTools()
        {
            if (IsMac)
            {
                return "Visual Studio build tools are not required on macOS.\n";
            }

            if (TryResolveWindowsFluidX3DBuildTools(out string details))
            {
                return details + "\n";
            }

            string bootstrapperPath = Path.Combine(
                Path.GetTempPath(),
                FluidX3DAblWorkflow.WindowsBuildToolsBootstrapperFileName);
            string installerArguments = FluidX3DAblWorkflow.GetWindowsBuildToolsInstallerArguments();

            try
            {
                using (var client = new WebClient())
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    client.DownloadFile(FluidX3DAblWorkflow.WindowsBuildToolsDownloadUrl, bootstrapperPath);
                }

                if (!File.Exists(bootstrapperPath))
                {
                    return "Error: Visual Studio Build Tools bootstrapper download failed.\n";
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = bootstrapperPath,
                    Arguments = installerArguments,
                    UseShellExecute = true,
                    Verb = "runas"
                });

                return "Visual Studio Build Tools installer launched from " + bootstrapperPath + ".\n"
                    + "Arguments: " + installerArguments + "\n"
                    + "Finish the installation, then rerun Install FluidX3D.\n";
            }
            catch (Exception ex)
            {
                return "Error installing Visual Studio C++ Build Tools for FluidX3D: " + ex.Message + "\n";
            }
        }

        private static bool TryResolveGitHeadCommit(string repositoryRoot, out string commit)
        {
            commit = null;

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "-C \"" + repositoryRoot + "\" rev-parse HEAD",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    if (process == null)
                    {
                        return false;
                    }

                    string stdOut = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(10000);
                    if (process.ExitCode != 0)
                    {
                        return false;
                    }

                    string value = (stdOut ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        return false;
                    }

                    commit = value;
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resources.Eddy3D_install;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("C440366E-09E2-4309-91EF-5B8B70851892"); }
        }
    }
}
