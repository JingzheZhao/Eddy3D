using System;
using System.IO;
using System.Linq;
using System.Text;

namespace EddyLib.Strings
{
    public static class PlotResiduals
    {
        /// <summary>
        /// Generates a gnuplot script that reads residuals.dat and creates a PNG plot
        /// </summary>
        public static string GenerateGnuplotScript(string residualsPath, string outputPngPath)
        {
            var sb = new StringBuilder();

            // Gnuplot script for PNG output
            sb.AppendLine("# Gnuplot script for OpenFOAM residuals");
            sb.AppendLine("set terminal pngcairo size 1920,1080 enhanced font 'Arial,14'");
            sb.AppendLine($"set output 'residuals.png'");
            sb.AppendLine();
            sb.AppendLine("# Styling");
            sb.AppendLine("set logscale y");
            sb.AppendLine("set format y \"10^{%T}\"");
            sb.AppendLine("set grid");
            sb.AppendLine("set key outside right top");
            sb.AppendLine();
            sb.AppendLine("# Labels");
            sb.AppendLine("set xlabel 'Iteration' font 'Arial,16'");
            sb.AppendLine("set ylabel 'Residual' font 'Arial,16'");
            sb.AppendLine("set title 'OpenFOAM Residuals Convergence' font 'Arial,18'");
            sb.AppendLine();
            sb.AppendLine("# Data file settings");
            sb.AppendLine("set datafile separator whitespace");
            sb.AppendLine("set datafile commentschars \"#\"");
            sb.AppendLine();

            // Read field names from the file if it exists
            string[] fieldNames = new string[] { "p", "U", "k", "epsilon", "omega", "nut" };
            if (File.Exists(residualsPath))
            {
                try
                {
                    var lines = File.ReadAllLines(residualsPath);
                    if (lines.Length > 1)
                    {
                        // Second line contains the header with field names
                        var headerLine = lines[1].TrimStart('#').Trim();
                        var fields = headerLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (fields.Length > 1)
                        {
                            // Skip first column (Time/Iteration)
                            fieldNames = fields.Skip(1).ToArray();
                        }
                    }
                }
                catch
                {
                    // Use default field names if reading fails
                }
            }

            sb.AppendLine("# Plot the data");
            sb.Append($"plot 'postProcessing/residuals/0/residuals.dat' using 1:2 with linespoints title '{fieldNames[0]}' lw 2 pt 7 ps 0.5");

            for (int i = 1; i < Math.Min(fieldNames.Length, 6); i++)
            {
                sb.AppendLine(", \\");
                sb.Append($"     '' using 1:{i + 2} with linespoints title '{fieldNames[i]}' lw 2 pt 7 ps 0.5");
            }
            sb.AppendLine();

            return sb.ToString();
        }

        /// <summary>
        /// Generates a batch file command to run gnuplot for residuals plotting
        /// </summary>
        public static string GeneratePlotCommand(string caseDir, int windDir)
        {
            var residualsPath = Path.Combine(caseDir, windDir.ToString(), "postProcessing", "residuals", "0", "residuals.dat");
            var outputPng = Path.Combine(caseDir, windDir.ToString(), "residuals.png");
            var gnuplotScript = Path.Combine(caseDir, windDir.ToString(), "plot_residuals.plt");

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("REM Plot residuals");
            sb.AppendLine($"if exist \"{residualsPath}\" (");
            sb.AppendLine($"    echo Generating residuals plot...");
            sb.AppendLine($"    gnuplot \"{gnuplotScript}\"");
            sb.AppendLine($"    if exist \"{outputPng}\" (");
            sb.AppendLine($"        echo Residuals plot saved to: {outputPng}");
            sb.AppendLine($"    ) else (");
            sb.AppendLine($"        echo Warning: Failed to generate residuals plot");
            sb.AppendLine($"    )");
            sb.AppendLine($") else (");
            sb.AppendLine($"    echo Warning: residuals.dat not found, skipping plot generation");
            sb.AppendLine($")");

            return sb.ToString();
        }
    }
}

