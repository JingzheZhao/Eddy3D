using System.Diagnostics;
using System.Xml.Linq;
using EddyLib;

namespace TemplateSync.Cli;

internal static partial class Program
{
    private static string FindSolutionRoot()
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var current = new DirectoryInfo(start);
            while (current != null)
            {
                if (File.Exists(Path.Combine(current.FullName, "Eddy.sln")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }
        }

        throw new InvalidOperationException("Could not locate solution root containing Eddy.sln.");
    }

    private static string GetDefaultTemplateRepoDir(string solutionRoot)
    {
        var userFolderPath = TryGetDefaultRepoDirFromUserFolders();
        if (!string.IsNullOrWhiteSpace(userFolderPath))
        {
            return userFolderPath;
        }

        return Path.GetFullPath(Path.Combine(solutionRoot, "..", "Eddy3D-OutdoorIndoorTemplates"));
    }

    private static void EnsureLocalTemplateRepo(string repoDir, string repoUrl, string branch, bool noGitUpdate)
    {
        if (!Directory.Exists(repoDir))
        {
            var parent = Directory.GetParent(repoDir);
            if (parent == null)
            {
                throw new DirectoryNotFoundException($"Parent directory for repo does not exist: {repoDir}");
            }

            Directory.CreateDirectory(parent.FullName);

            Console.WriteLine($"Cloning template repo into: {repoDir}");
            RunGit(parent.FullName, "clone", repoUrl, repoDir);
        }

        if (!Directory.Exists(Path.Combine(repoDir, ".git")))
        {
            throw new InvalidOperationException($"Path exists but is not a git repo: {repoDir}");
        }

        if (noGitUpdate)
        {
            if (!LocalBranchExists(repoDir, branch))
            {
                throw new InvalidOperationException(
                    $"Branch '{branch}' does not exist locally in {repoDir}. Remove --no-git-update to fetch it.");
            }

            RunGit(repoDir, "checkout", branch);
            return;
        }

        RunGit(repoDir, "fetch", "--all", "--prune");

        if (LocalBranchExists(repoDir, branch))
        {
            RunGit(repoDir, "checkout", branch);
        }
        else
        {
            if (!RemoteBranchExists(repoDir, branch))
            {
                throw new InvalidOperationException($"Remote branch '{branch}' not found in template repository.");
            }

            RunGit(repoDir, "checkout", "-B", branch, $"origin/{branch}");
        }

        RunGit(repoDir, "pull", "--ff-only", "origin", branch);
    }

    private static bool LocalBranchExists(string repoDir, string branch)
    {
        var code = RunGitCapture(repoDir, out var output, "branch", "--list", branch);
        return code == 0 && !string.IsNullOrWhiteSpace(output);
    }

    private static bool RemoteBranchExists(string repoDir, string branch)
    {
        var code = RunGitCapture(repoDir, out _, "ls-remote", "--exit-code", "--heads", "origin", branch);
        return code == 0;
    }

    private static void RunGit(string workingDir, params string[] args)
    {
        var code = RunGitCapture(workingDir, out var output, args);
        if (code == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            $"git {string.Join(" ", args)} failed with exit code {code}.\n{output}");
    }

    private static int RunGitCapture(string workingDir, out string combinedOutput, params string[] args)
    {
        var psi = new ProcessStartInfo("git")
        {
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        using var proc = Process.Start(psi)
                         ?? throw new InvalidOperationException("Failed to start git process.");
        var stdOut = proc.StandardOutput.ReadToEnd();
        var stdErr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();

        combinedOutput = (stdOut + "\n" + stdErr).Trim();
        return proc.ExitCode;
    }

    private static bool TryParseOptions(string[] args, out Options options)
    {
        options = new Options
        {
            TemplateRepoDir = Environment.GetEnvironmentVariable("EDDY_TEMPLATE_REPO_DIR") ?? string.Empty
        };

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--repo-dir":
                case "-r":
                    options.TemplateRepoDir = ReadRequiredValue(args, ref i, arg);
                    break;
                case "--template-root":
                case "-t":
                    options.TemplateRoot = ReadRequiredValue(args, ref i, arg);
                    break;
                case "--repo-url":
                    options.RepoUrl = ReadRequiredValue(args, ref i, arg);
                    break;
                case "--branch":
                case "-b":
                    options.Branch = ReadRequiredValue(args, ref i, arg);
                    break;
                case "--no-git-update":
                    options.NoGitUpdate = true;
                    break;
                case "--solution-root":
                case "-s":
                    options.SolutionRoot = ReadRequiredValue(args, ref i, arg);
                    break;
                case "--max-report":
                    options.MaxReport = int.Parse(ReadRequiredValue(args, ref i, arg));
                    break;
                case "--fix":
                    options.Fix = true;
                    break;
                case "--fix-counts":
                    options.FixCounts = true;
                    break;
                case "--verbose":
                case "-v":
                    options.Verbose = true;
                    break;
                case "--help":
                case "-h":
                    return false;
                default:
                    Console.Error.WriteLine($"Unknown argument: {arg}");
                    PrintUsage();
                    return false;
            }
        }

        if (options.MaxReport <= 0)
        {
            Console.Error.WriteLine("--max-report must be > 0");
            return false;
        }

        return true;
    }

    private static string ReadRequiredValue(string[] args, ref int index, string optionName)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing value for {optionName}");
        }

        index++;
        return args[index];
    }

    private static void PrintUsage()
    {
        var defaultRepoDir = TryGetDefaultRepoDirFromUserFolders()
                             ?? "<user-documents>/GitHub/Eddy3D-Dev/Eddy3D-OutdoorIndoorTemplates";

        Console.WriteLine("TemplateSync.Cli - check or sync Eddy component IO labels in .ghx templates");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  TemplateSync.Cli [--repo-dir <path>] [--template-root <path>] [--branch <name>] [--fix] [--fix-counts]");
        Console.WriteLine("                  [--no-git-update] [--solution-root <path>] [--max-report <n>] [--verbose]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --repo-dir, -r        Local template git repository directory.");
        Console.WriteLine($"                        Default: {defaultRepoDir}");
        Console.WriteLine("                        Override with EDDY_TEMPLATE_REPO_DIR.");
        Console.WriteLine("  --repo-url            Remote git URL for clone/fetch.");
        Console.WriteLine($"                        Default: {DefaultRepoUrl}");
        Console.WriteLine("  --branch, -b          Template repo branch to checkout/update.");
        Console.WriteLine($"                        Default: EddyVersion.ProductVersion ({EddyVersion.ProductVersion})");
        Console.WriteLine("  --no-git-update       Do not fetch/pull. Requires the branch to exist locally.");
        Console.WriteLine("  --template-root, -t   Optional subfolder to scan for .ghx files (defaults to repo root).");
        Console.WriteLine("  --fix                 Apply safe fixes to Name/NickName labels in template files.");
        Console.WriteLine("  --fix-counts          Guarded port count reconciliation using in-repo reference components.");
        Console.WriteLine("  --solution-root, -s   Explicit Eddy solution root (contains Eddy.sln).");
        Console.WriteLine("  --max-report          Max mismatches to print (default: 200).");
        Console.WriteLine("  --verbose, -v         Print per-file scan/update details.");
    }

    private static string? TryGetDefaultRepoDirFromUserFolders()
    {
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (!string.IsNullOrWhiteSpace(documents))
        {
            return Path.Combine(documents, "GitHub", "Eddy3D-Dev", "Eddy3D-OutdoorIndoorTemplates");
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(home))
        {
            return Path.Combine(home, "Documents", "GitHub", "Eddy3D-Dev", "Eddy3D-OutdoorIndoorTemplates");
        }

        return null;
    }

    private sealed class Options
    {
        public string TemplateRepoDir { get; set; } = string.Empty;
        public string TemplateRoot { get; set; } = string.Empty;
        public string RepoUrl { get; set; } = DefaultRepoUrl;
        public string Branch { get; set; } = EddyVersion.ProductVersion;
        public bool NoGitUpdate { get; set; }
        public string? SolutionRoot { get; set; }
        public bool Fix { get; set; }
        public bool FixCounts { get; set; }
        public bool Verbose { get; set; }
        public int MaxReport { get; set; } = 200;
    }

    private sealed class ParameterDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string NickName { get; set; } = string.Empty;
    }

    private sealed class ParameterSlot
    {
        public XElement ItemsElement { get; set; } = null!;
        public string Name { get; set; } = string.Empty;
        public string NickName { get; set; } = string.Empty;
    }

    private sealed class ComponentIoDefinition
    {
        public Guid ComponentGuid { get; set; }
        public List<ParameterDefinition> Inputs { get; set; } = new();
        public List<ParameterDefinition> Outputs { get; set; } = new();
    }

    private sealed class TemplateComponentUsage
    {
        public string TemplatePath { get; set; } = string.Empty;
        public string ComponentName { get; set; } = string.Empty;
        public Guid ComponentGuid { get; set; }
        public XElement? ParameterChunksElement { get; set; }
        public List<ParameterSlot> Inputs { get; set; } = new();
        public List<ParameterSlot> Outputs { get; set; } = new();
    }

    private sealed class ComponentParameterChunkReference
    {
        public string SourceTemplatePath { get; set; } = string.Empty;
        public List<XElement> InputChunks { get; set; } = new();
        public List<XElement> OutputChunks { get; set; } = new();
    }
}
