using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EddyLib.Compute
{
    public class SLURM_Runner
    {
        public SLURM_Runner(OFResult RES, string chargeAccount, string notificationEmail, int durationOfJob, int memPerCPU, string OFloadCommand)
        {
            this.CaseFolder = RES.WorkingDirectory;
            this.WindDirs = RES.Domain.BCond.WindDirections;
            this.NumCPUs = RES.RunSettings.CPUs;
            this.ChargeAccount = chargeAccount;
            this.NotificationEmail = notificationEmail;
            this.RAMperCPU = memPerCPU;
            this.DurationOfJob = durationOfJob;
            this.OFloadCommand = OFloadCommand;
            Result = "";

            Export_SLURM_Files();
        }

        // Properties changed to internal
        internal string CaseFolder { get; set; }

        internal string ChargeAccount { get; set; }
        internal int DurationOfJob { get; set; }
        internal string NotificationEmail { get; set; }
        internal int NumCPUs { get; set; }
        internal string OFloadCommand { get; set; }
        internal int RAMperCPU { get; set; }
        internal List<int> WindDirs { get; set; }

        public string Result { get; set; }

        private void CreateFile(string dirPath, string fileName, string content)
        {
            string filePath = System.IO.Path.Combine(dirPath, fileName);
            System.IO.File.WriteAllText(filePath, content.Replace("\r\n", "\n"));
        }

        private void Export_SLURM_Files()
        {
            try
            {
                // Extract the last folder name from the path
                string lastFolderName = GetLastFolderName(CaseFolder);

                // Ensure the parent directory exists
                if (!System.IO.Directory.Exists(CaseFolder))
                {
                    System.IO.Directory.CreateDirectory(CaseFolder);
                }

                List<string> runRelativePaths = new List<string>(); // List to store relative paths of run*.sh files
                List<string> simRelativePaths = new List<string>(); // List to store relative paths of sim*.sh files

                foreach (int number in WindDirs)
                {
                    // Create directory for each wind direction
                    string dirPath = System.IO.Path.Combine(CaseFolder, number.ToString());
                    if (!System.IO.Directory.Exists(dirPath))
                    {
                        System.IO.Directory.CreateDirectory(dirPath);
                    }

                    // Create "decompose", "run", and "sim" files
                    string decompFileName = string.Format("{0}_{1}_{2}.sh", "decompose", number, lastFolderName);
                    string runFileName = string.Format("{0}_{1}_{2}.sh", "run", number, lastFolderName);
                    string simFileName = string.Format("{0}_{1}_{2}.sh", "sim", number, lastFolderName);

                    string decompContent = GetDecomposeContent();
                    string runContent = GetRunContent(decompFileName, simFileName, runFileName);
                    string simContent = GetSimContent();

                    CreateFile(dirPath, decompFileName, decompContent);
                    CreateFile(dirPath, runFileName, runContent);
                    CreateFile(dirPath, simFileName, simContent);

                    // Store the relative path of the run file for global run file creation
                    string runRelativePath = System.IO.Path.Combine(number.ToString(), runFileName);
                    string simRelativePath = System.IO.Path.Combine(number.ToString(), simFileName);
                    runRelativePaths.Add(runRelativePath);
                    simRelativePaths.Add(simRelativePath);
                }

                // Create the global run file
                string globalRunFilename = "global_run_all.sh";
                string globalRunFilePath = System.IO.Path.Combine(CaseFolder, globalRunFilename);
                string globalRunContent = GetGlobalRunContent(runRelativePaths);
                CreateFile(CaseFolder, globalRunFilePath, globalRunContent.ToString());

                // Create the global sim file
                string globalSimFilename = "global_sim_all.sh";
                string globalSimFilePath = System.IO.Path.Combine(CaseFolder, globalSimFilename);
                string globalSimContent = GetGlobalSimContent(simRelativePaths);
                CreateFile(CaseFolder, globalSimFilePath, globalSimContent.ToString());

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
            return RemoveLeadingSpaces(rawContent);
        }

        private string GetGlobalRunContent(List<string> runRelativePaths)
        {
            StringBuilder content = new StringBuilder();

            content.AppendLine("#!/bin/bash");
            content.AppendLine("# Global script to run all simulations");

            // Add commands to change permissions of .sh files in subfolders to executable

            content.AppendLine("# Change permissions of all .sh files in subfolders to executable");
            content.AppendLine("find . -type f -name \"*.sh\" -exec chmod +x {} \\;");

            foreach (string relPath in runRelativePaths)
            {
                string directoryPath = System.IO.Path.GetDirectoryName(relPath);
                string filePath = System.IO.Path.GetFileName(relPath);

                content.AppendLine(string.Format("cd \"{0}\"", directoryPath));  // Change to the directory of the script
                content.AppendLine(string.Format("./{0}", filePath));   // Execute the sbatch command
                content.AppendLine("cd ..");                                     // Go back to the parent directory
            }

            // Remove leading spaces from each line
            return RemoveLeadingSpaces(content.ToString());
        }

        private string GetGlobalSimContent(List<string> simRelativePaths)
        {
            StringBuilder content = new StringBuilder();

            content.AppendLine("#!/bin/bash");
            content.AppendLine("# Global script to sim all simulations");

            // Add commands to change permissions of .sh files in subfolders to executable

            content.AppendLine("# Change permissions of all .sh files in subfolders to executable");
            content.AppendLine("find . -type f -name \"*.sh\" -exec chmod +x {} \\;");

            foreach (string relPath in simRelativePaths)
            {
                string directoryPath = System.IO.Path.GetDirectoryName(relPath);
                string filePath = System.IO.Path.GetFileName(relPath);

                content.AppendLine(string.Format("cd \"{0}\"", directoryPath));  // Change to the directory of the script
                content.AppendLine(string.Format("sbatch ./{0}", filePath));   // Execute the sbatch command
                content.AppendLine("cd ..");                                     // Go back to the parent directory
            }

            // Remove leading spaces from each line
            return RemoveLeadingSpaces(content.ToString());
        }

        private string GetLastFolderName(string path)
        {
            var directoryInfo = new System.IO.DirectoryInfo(path);
            return directoryInfo.Name;
        }

        private string GetRunContent(string decomposeFileName, string simFileName, string runFileName)
        {
            string rawContent = string.Format(
              "#!/bin/bash\n" +
              "jid_pre=$(sbatch --account {2} --parsable {0})\n" +
              "jid_w01=$(sbatch --account {2} --parsable --dependency=afterok:${{jid_pre}} {1})\n" +
              "sbatch --account {2} --dependency=afterok:${{jid_w01}} {3}",
              decomposeFileName,
              simFileName,
              this.ChargeAccount,
              runFileName
              );

            return RemoveLeadingSpaces(rawContent);
        }

        private string GetSimContent()
        {
            string rawContent = string.Format(@"#!/bin/bash
      #SBATCH --account={0}                     # charge account
      #SBATCH -N{1} --ntasks-per-node={1}       # Number of nodes and cores per node required
      #SBATCH --mem-per-cpu={2}G                 # Memory per core in GB
      #SBATCH -t{3}:00:00                       # Duration of the job (Ex: 1 hour)
      #SBATCH -qinferno                         # QOS Name
      #SBATCH -oReport-run-%j.out               # Combined output and error messages file
      #SBATCH --mail-type=FAIL                  # Mail preferences
      #SBATCH --mail-user={4}                   # E-mail address for notifications

      {5}
      srun simpleFoam -parallel | tee -a log.simpleFoam
      reconstructPar -latestTime | tee -a log.reconstruct
      ", this.ChargeAccount, (int)Math.Sqrt(this.NumCPUs), this.RAMperCPU, this.DurationOfJob, this.NotificationEmail, this.OFloadCommand).Replace("\r\n", "\n"); ;

            return RemoveLeadingSpaces(rawContent);
        }

        private string RemoveLeadingSpaces(string text)
        {
            // Remove leading spaces from each line

            return string.Join("\n", text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                          .Select(line => line.TrimStart()));
        }
    }
}