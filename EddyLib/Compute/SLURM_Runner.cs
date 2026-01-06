using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EddyLib.Compute
{
    public class SLURM_Runner
    {
        public SLURM_Runner(OFResult RES, string chargeAccount, string notificationEmail, int durationOfJob, int memPerCPU, string OFloadCommand)
        {
            CaseFolder = RES.WorkingDirectory;
            WindDirs = RES.Domain.BCond.WindDirections;
            NumCPUs = RES.RunSettings.CPUs;
            ChargeAccount = chargeAccount;
            NotificationEmail = notificationEmail;
            RAMperCPU = memPerCPU;
            DurationOfJob = durationOfJob;
            OFloadCommand = OFloadCommand;
            Result = string.Empty;

            Export_SLURM_Files();
        }

        // Properties changed to internal
        internal string CaseFolder { get; private set; }

        internal string ChargeAccount { get; private set; }
        internal int DurationOfJob { get; private set; }
        internal string NotificationEmail { get; private set; }
        internal int NumCPUs { get; private set; }
        internal string OFloadCommand { get; private set; }
        internal int RAMperCPU { get; private set; }
        internal List<int> WindDirs { get; private set; }

        public string Result { get; set; }

        private static void WriteScript(string filePath, string content)
        {
            File.WriteAllText(filePath, NormalizeLineEndings(content));
        }

        private void Export_SLURM_Files()
        {
            try
            {
                string caseFolderName = GetCaseFolderName(CaseFolder);
                Directory.CreateDirectory(CaseFolder);

                var runRelativePaths = new List<string>(); // List to store relative paths of run*.sh files
                var simRelativePaths = new List<string>(); // List to store relative paths of sim*.sh files
                var reconstructRelativePaths = new List<string>(); // List to store relative paths of reconstruct*.sh files

                foreach (int number in WindDirs)
                {
                    // Create directory for each wind direction
                    string dirPath = Path.Combine(CaseFolder, number.ToString());
                    Directory.CreateDirectory(dirPath);

                    // Create "decompose", "run", and "sim" files
                    string decompFileName = BuildScriptName("decompose", number, caseFolderName);
                    string runFileName = BuildScriptName("run", number, caseFolderName);
                    string simFileName = BuildScriptName("sim", number, caseFolderName);
                    string reconstructFileName = BuildScriptName("reconstruct", number, caseFolderName);

                    string decompContent = GetDecomposeContent();
                    string runContent = GetRunContent(decompFileName, simFileName, reconstructFileName);
                    string simContent = GetSimContent();
                    string reconstructContent = GetReconstructContent();

                    WriteScript(Path.Combine(dirPath, decompFileName), decompContent);
                    WriteScript(Path.Combine(dirPath, runFileName), runContent);
                    WriteScript(Path.Combine(dirPath, simFileName), simContent);
                    WriteScript(Path.Combine(dirPath, reconstructFileName), reconstructContent);

                    // Store the relative path of the run file for global run file creation
                    string runRelativePath = Path.Combine(number.ToString(), runFileName);
                    string simRelativePath = Path.Combine(number.ToString(), simFileName);
                    string reconstructRelativePath = Path.Combine(number.ToString(), reconstructFileName);

                    runRelativePaths.Add(runRelativePath);
                    simRelativePaths.Add(simRelativePath);
                    reconstructRelativePaths.Add(reconstructRelativePath);
                }

                // Create the global run file
                string globalRunFilename = Path.Combine(CaseFolder, "global_run_all.sh");
                string globalRunContent = GetGlobalRunContent(runRelativePaths);
                WriteScript(globalRunFilename, globalRunContent);

                // Create the global sim file
                string globalSimFilename = Path.Combine(CaseFolder, "global_sim_all.sh");
                string globalSimContent = GetGlobalSimContent(simRelativePaths);
                WriteScript(globalSimFilename, globalSimContent);

                // Create the global reconstruct file
                string globalReconFilename = Path.Combine(CaseFolder, "global_reconstruct_all.sh");
                string globalReconContent = GetGlobalReconstructContent(reconstructRelativePaths);
                WriteScript(globalReconFilename, globalReconContent);

                // Set a success message on the component
                Result = "SLURM files created";
                //A = "Files created";
            }
            catch (Exception ex)
            {
                //// Set an error message on the component
                Result = "Error in creating SLURM files." + ex.Message;
                //A = ex.Message; // Output the error details to A
            }
        }

        // Functions to return the content for each type of file
        private string GetDecomposeContent()
        {
            string rawContent = string.Format(@"#!/bin/bash
                  #SBATCH --account={0}                       # charge account
                  #SBATCH -N1 --ntasks-per-node=1                 # Number of nodes and cores per node required
                  #SBATCH --mem-per-cpu=60G                       # Memory per core
                  #SBATCH -t1:00:00                               # Duration of the job (Ex: 1 hour)
                  #SBATCH -qinferno                               # QOS Name
                  #SBATCH -oReport-decompose-%j.out               # Combined output and error messages file
                  #SBATCH --mail-type=FAIL                        # Mail preferences
                  #SBATCH --mail-user={1}             # E-mail address for notifications

                  {2}
                  decomposePar -force | tee -a log.decompose
      ", this.ChargeAccount, this.NotificationEmail, this.OFloadCommand);

            // Remove leading spaces from each line
            return NormalizeScript(rawContent);
        }

        private string GetReconstructContent()
        {
            string rawContent = string.Format(@"#!/bin/bash
                  #SBATCH --account={0}                           # charge account
                  #SBATCH -N1 --ntasks-per-node=1                 # Number of nodes and cores per node required
                  #SBATCH --mem-per-cpu=50G                       # Memory per core
                  #SBATCH -t00:05:00                              # Duration of the job (Ex: 1 hour)
                  #SBATCH -qinferno                               # QOS Name
                  #SBATCH -oReport-decompose-%j.out               # Combined output and error messages file
                  #SBATCH --mail-type=FAIL                        # Mail preferences
                  #SBATCH --mail-user={1}                         # E-mail address for notifications

                  {2}
                  reconstructPar -latestTime | tee -a log.reconstruct
      ", this.ChargeAccount, this.NotificationEmail, this.OFloadCommand);

            // Remove leading spaces from each line
            return NormalizeScript(rawContent);
        }

        private string GetGlobalReconstructContent(List<string> relativePaths)
        {
            return BuildGlobalScript(
                "reconstruct",
                relativePaths,
                fileName => string.Format("sbatch ./{0}", fileName));
        }

        private string GetGlobalRunContent(List<string> runRelativePaths)
        {
            return BuildGlobalScript(
                "run",
                runRelativePaths,
                fileName => string.Format("./{0}", fileName));
        }

        private string GetGlobalSimContent(List<string> simRelativePaths)
        {
            return BuildGlobalScript(
                "sim",
                simRelativePaths,
                fileName => string.Format("sbatch ./{0}", fileName));
        }

        private static string GetCaseFolderName(string path)
        {
            var normalized = path?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) ?? string.Empty;
            return new DirectoryInfo(normalized).Name;
        }

        private string GetRunContent(string decomposeFileName, string simFileName, string reconstructFileName)
        {
            string rawContent = string.Format(
              "#!/bin/bash\n" +
              "jid_pre=$(sbatch --account {2} --parsable {0})\n" +
              "jid_w01=$(sbatch --account {2} --parsable --dependency=afterok:${{jid_pre}} {1})\n" +
              "sbatch --account {2} --dependency=afterok:${{jid_w01}} {3}",
              decomposeFileName,
              simFileName,
              this.ChargeAccount,
              reconstructFileName
              );

            return NormalizeScript(rawContent);
        }

        private string GetSimContent()
        {
            int tasksPerNode = Math.Max(1, (int)Math.Sqrt(NumCPUs));
            string rawContent = string.Format(@"#!/bin/bash
      #SBATCH --account={0}                     # charge account
      #SBATCH -N{1} --ntasks-per-node={1}       # Number of nodes and cores per node required
      #SBATCH --mem-per-cpu={2}G                # Memory per core in GB
      #SBATCH -t{3}:00:00                       # Duration of the job (Ex: 1 hour)
      #SBATCH -qinferno                         # QOS Name
      #SBATCH -oReport-run-%j.out               # Combined output and error messages file
      #SBATCH --mail-type=FAIL                  # Mail preferences
      #SBATCH --mail-user={4}                   # E-mail address for notifications

      {5}
      srun simpleFoam -parallel | tee -a log.simpleFoam
      reconstructPar -latestTime | tee -a log.reconstruct
      ", this.ChargeAccount, tasksPerNode, this.RAMperCPU, this.DurationOfJob, this.NotificationEmail, this.OFloadCommand);

            return NormalizeScript(rawContent);
        }

        private static string BuildGlobalScript(string label, IEnumerable<string> relativePaths, Func<string, string> commandBuilder)
        {
            var content = new StringBuilder();

            content.AppendLine("#!/bin/bash");
            content.AppendLine(string.Format("# Global script to {0} all simulations", label));
            content.AppendLine("# Change permissions of all .sh files in subfolders to executable");
            content.AppendLine("find . -type f -name \"*.sh\" -exec chmod +x {} \\;");

            foreach (string relPath in relativePaths)
            {
                string directoryPath = Path.GetDirectoryName(relPath);
                string fileName = Path.GetFileName(relPath);

                if (string.IsNullOrEmpty(directoryPath))
                {
                    continue;
                }

                content.AppendLine(string.Format("cd \"{0}\"", directoryPath));
                content.AppendLine(commandBuilder(fileName));
                content.AppendLine("cd ..");
            }

            return NormalizeScript(content.ToString());
        }

        private static string BuildScriptName(string prefix, int windDir, string caseFolderName)
        {
            return string.Format("{0}_{1}_{2}.sh", prefix, windDir, caseFolderName);
        }

        private static string NormalizeScript(string text)
        {
            return string.Join("\n", NormalizeLineEndings(text)
                .Split(new[] { "\n" }, StringSplitOptions.None)
                .Select(line => line.TrimStart()));
        }

        private static string NormalizeLineEndings(string text)
        {
            return text.Replace("\r\n", "\n").Replace("\r", "\n");
        }
    }
}