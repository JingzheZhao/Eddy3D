using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;

namespace Eddy
{
    public class InstallEngines_Component : GH_Component
    {
        public InstallEngines_Component()
          : base("Install Engines", "Install",
              "Downloads and installs required simulation engines (EnergyPlus v9.4.0, Radiance, & blueCFD-Core 2020-1).",
              EddyVersion.Name, "0 | Utilities")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Install EnergyPlus", "EP", "Set to True to download and launch EnergyPlus v9.4.0 installer.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Install Radiance", "Rad", "Set to True to download and install Radiance (v2020/012cb178).", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Install blueCFD", "CFD", "Set to True to download and launch blueCFD-Core 2020-1 installer.", GH_ParamAccess.item, false);
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

            // Clear previous messages
            this.ClearRuntimeMessages();

            // Check Installations
            System.Collections.Generic.List<string> missing = new System.Collections.Generic.List<string>();

            try { DefaultDirectoriesAndPaths.CheckRadiance(); }
            catch { missing.Add("Radiance"); }

            try { DefaultDirectoriesAndPaths.CheckEnergyPlus(); }
            catch { missing.Add("EnergyPlus"); }

            try { DefaultDirectoriesAndPaths.CheckBlueCfd(); }
            catch { missing.Add("blueCFD"); }

            string log = "";

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
                log += InstallBlueCfd();
            }

            DA.SetData(0, log);
        }

        private string InstallEnergyPlus()
        {
            string url = "https://github.com/NREL/EnergyPlus/releases/download/v9.4.0/EnergyPlus-9.4.0-998c4b761e-Windows-x86_64.exe";
            string tempFile = Path.Combine(Path.GetTempPath(), "EnergyPlus-9.4.0-Installer.exe");

            try
            {
                using (WebClient client = new WebClient())
                {
                    // Ensure TLS 1.2 is supported (GitHub requirement)
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    client.DownloadFile(url, tempFile);
                }

                if (File.Exists(tempFile))
                {
                    Process.Start(tempFile);
                    return $"EnergyPlus installer launched from {tempFile}. Please complete the installation manually.\n";
                }
                else
                {
                    return "Error: EnergyPlus installer download failed.\n";
                }
            }
            catch (Exception ex)
            {
                return $"Error installing EnergyPlus: {ex.Message}\n";
            }
        }

        private string InstallRadiance()
        {
            string url = "https://github.com/LBNL-ETA/Radiance/releases/download/012cb178/Radiance_012cb178_Windows.zip";
            string zipFile = Path.Combine(Path.GetTempPath(), "Radiance_012cb178_Windows.zip");
            
            string baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Eddy3D");
            string targetDir = Path.Combine(baseDir, "Radiance_012cb178_Windows");

            try
            {
                if (!Directory.Exists(baseDir)) Directory.CreateDirectory(baseDir);

                // Clean up previous installs to avoid "File already exists" during unzip
                if (Directory.Exists(targetDir))
                {
                    Directory.Delete(targetDir, true);
                }
                Directory.CreateDirectory(targetDir);
                
                // Download
                using (WebClient client = new WebClient())
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    client.DownloadFile(url, zipFile);
                }

                // Extract
                ZipFile.ExtractToDirectory(zipFile, targetDir);

                return $"Radiance installed successfully to {targetDir}.\n";
            }
            catch (Exception ex)
            {
                return $"Error installing Radiance: {ex.Message}\n";
            }
        }

        private string InstallBlueCfd()
        {
            string url = "https://github.com/blueCFD/Core/releases/download/blueCFD-Core-2020-1/blueCFD-Core-2020-1-win64-setup.exe";
            string tempFile = Path.Combine(Path.GetTempPath(), "blueCFD-Core-2020-1-Installer.exe");

            try
            {
                using (WebClient client = new WebClient())
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    client.DownloadFile(url, tempFile);
                }

                if (File.Exists(tempFile))
                {
                    Process.Start(tempFile);
                    return $"blueCFD-Core installer launched from {tempFile}. Please complete the installation manually.\n";
                }
                else
                {
                    return "Error: blueCFD-Core installer download failed.\n";
                }
            }
            catch (Exception ex)
            {
                return $"Error installing blueCFD: {ex.Message}\n";
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
