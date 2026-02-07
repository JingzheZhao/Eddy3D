using System;
using System.Collections.Generic;
using System.Text;

namespace EddyLib.Docker
{
    /// <summary>
    /// Generates Docker-based batch/shell scripts for OpenFOAM execution.
    /// Replaces the old DockerPrefixPath() approach that created one container per command
    /// with a single-container script that runs all commands sequentially.
    /// </summary>
    public static class DockerBatchScriptBuilder
    {
        /// <summary>
        /// Builds a shell script (.sh) that runs all OpenFOAM commands in a single Docker container.
        /// </summary>
        /// <param name="commands">OpenFOAM commands (e.g., "blockMesh", "snappyHexMesh -overwrite").</param>
        /// <param name="hostCasePath">Host filesystem path to the case directory.</param>
        /// <returns>Shell script content.</returns>
        public static string BuildDockerShellScript(IEnumerable<string> commands, string hostCasePath)
        {
            var dockerExe = DockerEnvironment.GetDockerPath() ?? "/usr/local/bin/docker";
            var bashCmd = DockerRunner.BuildCommandChain(new List<string>(commands));
            var escapedCmd = bashCmd.Replace("'", "'\"'\"'");

            var sb = new StringBuilder();
            sb.AppendLine("#!/bin/bash");
            sb.AppendLine("# Auto-generated Eddy3D Docker execution script");
            sb.AppendLine(string.Format("# Generated: {0}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            sb.AppendLine();
            sb.AppendLine("export PATH=\"/usr/local/bin:/opt/homebrew/bin:/Applications/Docker.app/Contents/Resources/bin:$HOME/.docker/bin:$PATH\"");
            sb.AppendLine();
            sb.AppendLine(string.Format("echo \"Eddy3D Docker Runner\""));
            sb.AppendLine(string.Format("echo \"Image: {0}\"", DockerConfig.ImageName));
            sb.AppendLine(string.Format("echo \"Case: {0}\"", hostCasePath));
            sb.AppendLine("echo \"----------------------------------------\"");
            sb.AppendLine();
            sb.AppendLine(string.Format(
                "{0} run --rm -it --platform {1} --entrypoint /bin/bash \\",
                dockerExe, DockerConfig.Platform));
            sb.AppendLine(string.Format(
                "  -v \"{0}:{1}\" \\",
                hostCasePath, DockerConfig.CaseMountPoint));
            sb.AppendLine(string.Format(
                "  -w {0} {1} \\",
                DockerConfig.CaseMountPoint, DockerConfig.ImageName));
            sb.AppendLine(string.Format("  -c '{0}'", escapedCmd));
            sb.AppendLine();
            sb.AppendLine("echo \"\"");
            sb.AppendLine("echo \"----------------------------------------\"");
            sb.AppendLine("echo \"Docker execution complete.\"");

            return sb.ToString();
        }

        /// <summary>
        /// Builds a Windows batch (.bat) wrapper that calls Docker to run OpenFOAM commands.
        /// For backward compatibility with Eddy3D's batch file execution pattern.
        /// </summary>
        /// <param name="commands">OpenFOAM commands to execute.</param>
        /// <param name="hostCasePath">Host filesystem path to the case directory.</param>
        /// <returns>Batch file content.</returns>
        public static string BuildDockerBatWrapper(IEnumerable<string> commands, string hostCasePath)
        {
            var dockerExe = DockerEnvironment.GetDockerPath() ?? "docker";
            var bashCmd = DockerRunner.BuildCommandChain(new List<string>(commands));

            var sb = new StringBuilder();
            sb.AppendLine("@echo off");
            sb.AppendLine("REM Auto-generated Eddy3D Docker execution script");
            sb.AppendLine(string.Format("REM Generated: {0}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            sb.AppendLine();
            sb.AppendLine("echo Eddy3D Docker Runner");
            sb.AppendLine(string.Format("echo Image: {0}", DockerConfig.ImageName));
            sb.AppendLine(string.Format("echo Case: {0}", hostCasePath));
            sb.AppendLine("echo ----------------------------------------");
            sb.AppendLine();
            sb.AppendLine(string.Format(
                "\"{0}\" run --rm --platform {1} --entrypoint /bin/bash -v \"{2}:{3}\" -w {3} {4} -c \"{5}\"",
                dockerExe,
                DockerConfig.Platform,
                hostCasePath,
                DockerConfig.CaseMountPoint,
                DockerConfig.ImageName,
                bashCmd.Replace("\"", "\\\"")));
            sb.AppendLine();
            sb.AppendLine("echo.");
            sb.AppendLine("echo ----------------------------------------");
            sb.AppendLine("echo Docker execution complete.");

            return sb.ToString();
        }
    }
}
