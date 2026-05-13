using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EddyLib
{
    /// <summary>
    /// Splits large probe-point sets into multiple smaller probe dictionaries that can run in
    /// parallel as separate <c>foamPostProcess</c> processes, then re-stitches the per-chunk
    /// result files into the canonical single result file that the existing parser expects.
    ///
    /// The OpenFOAM 12 probes function object has a per-probe inner loop that doesn't scale
    /// well on Windows — running K processes against K subsets gives roughly K× throughput on
    /// the variable cost, bounded by the fixed mesh-load cost paid per process.
    /// </summary>
    internal static class ProbeChunking
    {
        // Below this point count, the fixed cost (mesh load per process) dominates and chunking
        // is a net loss. Empirically: a single 60-second run becomes >60s when split into 8.
        private const int MinPointsToChunk = 5000;

        // Cap on parallel processes regardless of CPU count — past ~8, disk contention from
        // simultaneous polyMesh reads (junctioned, in this case) outweighs the per-probe speedup.
        private const int MaxChunks = 8;

        internal const string ChunkSuffix = "__c";

        internal static int DecideChunkCount(int pointCount, int cpus)
        {
            if (pointCount < MinPointsToChunk) return 1;
            int cap = Math.Max(1, Math.Min(MaxChunks, cpus));
            return Math.Min(cap, Math.Max(1, pointCount / (MinPointsToChunk / 2)));
        }

        internal static string ChunkName(string probeName, int chunkIndex)
        {
            return probeName + ChunkSuffix + chunkIndex;
        }

        internal static List<List<Point3d>> Split(IReadOnlyList<Point3d> points, int chunkCount)
        {
            // Sequential (contiguous) split rather than interleaved (modulo). This matters
            // because the merge step concatenates chunk results in chunk-index order, so
            // contiguous splitting preserves the original probe-point ordering in the merged
            // output. Interleaved splitting would shuffle results.
            var result = new List<List<Point3d>>(chunkCount);
            int n = points.Count;
            int baseSize = n / chunkCount;
            int remainder = n % chunkCount;
            int cursor = 0;
            for (int i = 0; i < chunkCount; i++)
            {
                int take = baseSize + (i < remainder ? 1 : 0);
                var chunk = new List<Point3d>(take);
                for (int j = 0; j < take; j++)
                {
                    chunk.Add(points[cursor + j]);
                }
                result.Add(chunk);
                cursor += take;
            }
            return result;
        }

        /// <summary>
        /// Builds the PowerShell one-liner that launches K parallel foamPostProcess processes
        /// and waits for all to exit. Runs inside the BlueCFD batch context, so foamPostProcess
        /// is on PATH from setvars.bat.
        /// </summary>
        /// <summary>
        /// Writes a PowerShell script to a sidecar .ps1 file in <paramref name="workDir"/> that
        /// launches K parallel postProcess/foamPostProcess processes and waits for them. Returns
        /// the short cmd-line invocation (powershell -File …) that runs the script.
        ///
        /// Sidecar approach is necessary because the surrounding BlueCfdScriptBuilder wraps every
        /// command with `echo Running: <line>`, and cmd parses the entire echo line for pipes
        /// and parens *before* running echo. Inline PowerShell bodies contain enough `(`, `)`,
        /// and `|` to make cmd think the line is malformed and abort the whole batch.
        /// </summary>
        internal static string BuildParallelLaunchCommand(string workDir, string exeName, string caseRelativeDir, string probeName, int chunkCount, int latestTime)
        {
            string scriptName = "_eddy_probe_" + caseRelativeDir + "_" + probeName + ".ps1";
            string scriptPath = Path.Combine(workDir, scriptName);

            var ps = new StringBuilder();
            ps.AppendLine("$ErrorActionPreference='Stop'");
            ps.Append("$exe = (Get-Command '").Append(exeName).AppendLine(".exe').Source");
            ps.Append("$case = '").Append(caseRelativeDir).AppendLine("'");
            ps.Append("$probe = '").Append(probeName).AppendLine("'");
            ps.Append("$K = ").AppendLine(chunkCount.ToString());
            ps.Append("$time = '").Append(latestTime).AppendLine("'");
            ps.AppendLine("$procs = @()");
            ps.AppendLine("for ($i = 0; $i -lt $K; $i++) {");
            ps.Append("    $cn = $probe + '").Append(ChunkSuffix).AppendLine("' + $i");
            ps.AppendLine("    $pp = Join-Path $case (Join-Path 'postProcessing' $cn)");
            ps.AppendLine("    if (Test-Path $pp) { Remove-Item -Recurse -Force $pp }");
            ps.Append("    $stdout = '").Append(exeName).AppendLine("_' + $cn + '.log'");
            ps.Append("    $stderr = '").Append(exeName).AppendLine("_' + $cn + '.err'");
            ps.AppendLine("    $procs += Start-Process -FilePath $exe -ArgumentList @('-case', $case, '-func', $cn, '-time', $time) -NoNewWindow -PassThru -RedirectStandardOutput $stdout -RedirectStandardError $stderr");
            ps.AppendLine("}");
            ps.AppendLine("$procs | Wait-Process");

            try
            {
                Directory.CreateDirectory(workDir);
                File.WriteAllText(scriptPath, ps.ToString());
            }
            catch
            {
                // Fall back to inlining if the script can't be written — at least probing still
                // tries to run (and any failure will surface as a clear error).
            }

            // Use the absolute path to powershell.exe: setvars.bat replaces PATH and removes
            // C:\Windows\System32 from it, so bare `powershell` fails with "not recognized".
            // -File argument is intentionally NOT quoted: PowerShell rejects quoted -File paths
            // ("Illegal characters in path"). scriptName is constructed from probe/case names
            // that we have already validated to be alphanumeric + underscore + dash + dot.
            return @"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\" + scriptName;
        }

        /// <summary>
        /// Merges K per-chunk probe result files into a single canonical result file at the
        /// path the existing parser expects. Strategy: copy chunk-0 verbatim, then for each
        /// subsequent chunk append its non-comment last-data-line value tokens (skipping the
        /// time column) to the merged file's data line.
        ///
        /// Returns true if a merged file was written (or already existed at the target path).
        /// </summary>
        internal static bool MergeChunkResults(string caseDir, string probeName, string fieldName, int chunkCount)
        {
            if (chunkCount <= 1) return false;

            string ppDir = Path.Combine(caseDir, "postProcessing");
            if (!Directory.Exists(ppDir)) return false;

            // Find the time-subfolder under chunk 0 (only one expected because we ran with -time).
            string chunk0Root = Path.Combine(ppDir, ChunkName(probeName, 0));
            if (!Directory.Exists(chunk0Root)) return false;

            string timeFolder = Directory.EnumerateDirectories(chunk0Root).FirstOrDefault();
            if (timeFolder == null) return false;
            string timeName = Path.GetFileName(timeFolder);

            string chunk0File = Path.Combine(timeFolder, fieldName);
            if (!File.Exists(chunk0File)) return false;

            string mergedDir = Path.Combine(ppDir, probeName, timeName);
            Directory.CreateDirectory(mergedDir);
            string mergedFile = Path.Combine(mergedDir, fieldName);

            // Read header + data line from each chunk.
            var chunkLines = new string[chunkCount];
            for (int i = 0; i < chunkCount; i++)
            {
                string chunkFile = Path.Combine(ppDir, ChunkName(probeName, i), timeName, fieldName);
                if (!File.Exists(chunkFile)) return false;
                chunkLines[i] = ReadLastDataLine(chunkFile);
                if (string.IsNullOrEmpty(chunkLines[i])) return false;
            }

            // Compose merged data line: time from chunk0, then the values from each chunk concatenated.
            var merged = new StringBuilder(chunkLines.Sum(s => s.Length) + 64);
            merged.Append(ExtractTimeToken(chunkLines[0])).Append(' ');
            for (int i = 0; i < chunkCount; i++)
            {
                merged.Append(StripTimeToken(chunkLines[i])).Append(' ');
            }

            // Copy chunk0's header (every leading '#' line) then write the merged data line.
            using (var sw = new StreamWriter(mergedFile, false))
            {
                foreach (var line in File.ReadLines(chunk0File))
                {
                    if (line.StartsWith("#", StringComparison.Ordinal)) sw.WriteLine(line);
                    else break;
                }
                sw.WriteLine(merged.ToString().TrimEnd());
            }

            return true;
        }

        private static string ReadLastDataLine(string path)
        {
            string last = null;
            foreach (var line in File.ReadLines(path))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.StartsWith("#", StringComparison.Ordinal)) continue;
                last = line;
            }
            return last;
        }

        private static string ExtractTimeToken(string dataLine)
        {
            int i = 0;
            while (i < dataLine.Length && char.IsWhiteSpace(dataLine[i])) i++;
            int start = i;
            while (i < dataLine.Length && !char.IsWhiteSpace(dataLine[i])) i++;
            return dataLine.Substring(start, i - start);
        }

        private static string StripTimeToken(string dataLine)
        {
            int i = 0;
            while (i < dataLine.Length && char.IsWhiteSpace(dataLine[i])) i++;
            while (i < dataLine.Length && !char.IsWhiteSpace(dataLine[i])) i++;
            while (i < dataLine.Length && char.IsWhiteSpace(dataLine[i])) i++;
            return i < dataLine.Length ? dataLine.Substring(i) : string.Empty;
        }
    }
}
