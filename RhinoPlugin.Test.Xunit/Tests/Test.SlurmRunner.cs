using EddyLib;
using EddyLib.Compute;
using Rhino.Geometry;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace RhinoPlugin.Test.Xunit
{
    [Collection("Rhino Collection")]
    public class SlurmRunnerTests
    {
        [NotWindowsServerFact]
        public void SlurmRunner_WritesScriptsPerWindDir()
        {
            var root = TestFixtures.CreateTestDirectory("testcase-slurm");
            try
            {
                var windDirs = new List<int> { 0, 90 };
                var bcCollection = TestFixtures.CreateDefaultBCCollection(windDirs);
                var domain = new OFBoxDomain(Setup.SetUpBuildingMesh(), new Mesh(), bcCollection, 20);
                var runSettings = TestFixtures.CreateDefaultRunSettings();
                var meshSettings = TestFixtures.CreateDefaultMeshSettings(root);
                var result = new OFResult(domain, runSettings, meshSettings, root);

                var runner = new SLURM_Runner(
                    result,
                    chargeAccount: "acct123",
                    notificationEmail: "user@example.com",
                    durationOfJob: 2,
                    memPerCPU: 12,
                    OFloadCommand: "module load openfoam");

                Assert.Equal("SLURM files created", runner.Result);

                var caseName = new DirectoryInfo(root).Name;

                foreach (var windDir in windDirs)
                {
                    var dirPath = Path.Combine(root, windDir.ToString());
                    Assert.True(Directory.Exists(dirPath));

                    var decompName = $"decompose_{windDir}_{caseName}.sh";
                    var runName = $"run_{windDir}_{caseName}.sh";
                    var simName = $"sim_{windDir}_{caseName}.sh";
                    var reconName = $"reconstruct_{windDir}_{caseName}.sh";

                    var decompPath = Path.Combine(dirPath, decompName);
                    var runPath = Path.Combine(dirPath, runName);
                    var simPath = Path.Combine(dirPath, simName);
                    var reconPath = Path.Combine(dirPath, reconName);

                    Assert.True(File.Exists(decompPath));
                    Assert.True(File.Exists(runPath));
                    Assert.True(File.Exists(simPath));
                    Assert.True(File.Exists(reconPath));

                    var runContent = File.ReadAllText(runPath);
                    Assert.Contains($"sbatch --account acct123 --parsable {decompName}", runContent);
                    Assert.Contains($"sbatch --account acct123 --parsable --dependency=afterok:${{jid_pre}} {simName}", runContent);
                    Assert.Contains($"sbatch --account acct123 --dependency=afterok:${{jid_w01}} {reconName}", runContent);

                    var decompContent = File.ReadAllText(decompPath);
                    Assert.Contains("decomposePar -force", decompContent);

                    var simContent = File.ReadAllText(simPath);
                    Assert.Contains("#SBATCH -N2 --ntasks-per-node=2", simContent);
                    Assert.Contains("srun foamRun -solver incompressibleFluid -parallel", simContent);

                    var reconContent = File.ReadAllText(reconPath);
                    Assert.Contains("reconstructPar -latestTime", reconContent);
                }

                var globalRun = File.ReadAllText(Path.Combine(root, "global_run_all.sh"));
                Assert.Contains("find . -type f -name \"*.sh\" -exec chmod +x {} \\;", globalRun);
                foreach (var windDir in windDirs)
                {
                    Assert.Contains($"cd \"{windDir}\"", globalRun);
                    Assert.Contains($"./run_{windDir}_{caseName}.sh", globalRun);
                }

                var globalSim = File.ReadAllText(Path.Combine(root, "global_sim_all.sh"));
                foreach (var windDir in windDirs)
                {
                    Assert.Contains($"sbatch ./sim_{windDir}_{caseName}.sh", globalSim);
                }

                var globalRecon = File.ReadAllText(Path.Combine(root, "global_reconstruct_all.sh"));
                foreach (var windDir in windDirs)
                {
                    Assert.Contains($"sbatch ./reconstruct_{windDir}_{caseName}.sh", globalRecon);
                }
            }
            finally
            {
                TestFixtures.CleanupTestDirectory(root);
            }
        }
    }
}
