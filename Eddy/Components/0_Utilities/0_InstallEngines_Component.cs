using Eddy.Properties;
using EddyLib;
using EddyLib.Docker;
using Grasshopper.Kernel;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;

namespace Eddy
{
    public class InstallEngines_Component : GH_Component
    {
        private static readonly bool IsMac = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public InstallEngines_Component()
          : base("Install Engines", "Install",
              IsMac
                  ? "Downloads and installs required simulation engines (EnergyPlus v9.4.0, Radiance, & Docker)."
                  : "Downloads and installs required simulation engines (EnergyPlus v9.4.0, Radiance, & blueCFD-Core 2020-1).",
              EddyVersion.Name, "0 | Utilities")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Install EnergyPlus", "EP", "Set to True to download and launch EnergyPlus v9.4.0 installer.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Install Radiance", "Rad", "Set to True to download and install Radiance.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter(
                IsMac ? "Install Docker" : "Install blueCFD",
                "CFD",
                IsMac ? "Set to True to open Docker Desktop download page."
                      : "Set to True to download and launch blueCFD-Core 2020-1 installer.",
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

            if (!DA.GetData(0, ref installEP)) return;
            if (!DA.GetData(1, ref installRad)) return;
            if (!DA.GetData(2, ref installCfd)) return;

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
                dockerDetails = "Docker is installed and running.";
            }
            catch (Exception ex)
            {
                dockerDetails = ex.Message;
                if (IsMac) missing.Add("Docker");
            }

            bool blueCfdInstalled = false;
            string blueCfdDetails;
            if (IsMac)
            {
                blueCfdDetails = "BlueCFD is not supported on macOS.";
            }
            else
            {
                try
                {
                    DefaultDirectoriesAndPaths.CheckBlueCfd();
                    blueCfdInstalled = true;
                    blueCfdDetails = "blueCFD is installed.";
                }
                catch (Exception ex)
                {
                    blueCfdDetails = ex.Message;
                    missing.Add("blueCFD");
                }
            }

            EngineInstallStatusCache.SetEngineStatus(snapshot, "Docker", dockerInstalled, dockerDetails);
            EngineInstallStatusCache.SetEngineStatus(snapshot, "BlueCFD", blueCfdInstalled, blueCfdDetails);
            EngineInstallStatusCache.SetEngineStatus(snapshot, "WSL", false, "WSL is not used by Eddy3D.");

            if (!EngineInstallStatusCache.TryWrite(EngineInstallStatusCache.EddyCachePath, snapshot, out string cacheWriteError))
            {
                string cacheWarning = "Install-check cache could not be written: " + cacheWriteError;
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, cacheWarning);
                log += cacheWarning + "\n";
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
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "open",
                            Arguments = string.Format("\"{0}\"", tempFile),
                            UseShellExecute = true
                        });
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

            string url = "https://github.com/LBNL-ETA/Radiance/releases/download/012cb178/Radiance_012cb178_Windows.zip";
            string zipFile = Path.Combine(Path.GetTempPath(), "Radiance_012cb178_Windows.zip");

            string baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Eddy3D");
            string targetDir = Path.Combine(baseDir, "Radiance_012cb178_Windows");

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
            string url = "https://github.com/LBNL-ETA/Radiance/releases/download/012cb178/Radiance_012cb178_OSX.zip";
            string zipFile = Path.Combine(Path.GetTempPath(), "Radiance_012cb178_OSX.zip");

            string baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Eddy3D");
            string targetDir = Path.Combine(baseDir, "Radiance_012cb178_OSX");

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
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "chmod",
                        Arguments = string.Format("-R +x \"{0}\"", binDir),
                        UseShellExecute = false,
                        CreateNoWindow = true
                    })?.WaitForExit(10000);
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

        private string InstallDocker()
        {
            try
            {
                if (DockerEnvironment.IsDockerAvailable())
                {
                    return "Docker is already installed and running.\n";
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = "https://www.docker.com/products/docker-desktop/",
                    UseShellExecute = true
                });

                return "Opened Docker Desktop download page. Please install Docker Desktop and start it.\n";
            }
            catch (Exception ex)
            {
                return string.Format("Error: {0}\nPlease install Docker Desktop from https://www.docker.com/products/docker-desktop/\n", ex.Message);
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
                    return string.Format("blueCFD-Core installer launched from {0}. Please complete the installation manually.\n", tempFile);
                }
                else
                {
                    return "Error: blueCFD-Core installer download failed.\n";
                }
            }
            catch (Exception ex)
            {
                return string.Format("Error installing blueCFD: {0}\n", ex.Message);
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
