using CommandLine;
using CommandLine.Text;
using EddyLib;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Linq;


namespace CallProbes
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // Check licence

            if (Utilities.CheckLicence() == true)
            {
                Options options = new Options();
                if (CommandLine.Parser.Default.ParseArguments(args, options))
                {
                    StringBuilder errorLog = new StringBuilder();

                    Console.WriteLine("Working directory: {0}", options.WorkingDir);
                    errorLog.AppendLine(string.Format("Working directory: {0}", options.WorkingDir));

                    Console.WriteLine("Probes: {0}", options.Probes);
                    errorLog.AppendLine(string.Format("Probes: {0}", options.Probes));

                    Console.WriteLine("Wind directions considered: {0}", options.WindDirs);
                    errorLog.AppendLine(string.Format("Wind directions considered: {0}", options.WindDirs));

                    Console.WriteLine("Mode (0=cp;1=U): {0}", options.Mode);
                    errorLog.AppendLine(string.Format("Mode (0=cp;1=U): {0}", options.Mode));

                    Console.WriteLine("Verbose: {0}", options.Verbose);
                    errorLog.AppendLine(string.Format("Verbose: {0}", options.Verbose));


                    var errorLogCalc = new StringBuilder();

                    BoundaryConditions bcond = new BoundaryConditions(BoundaryType.constant, options.WindDirs.Split(',').Select(Int32.Parse).ToList(), options.Uref, options.Z0, "");

                    WindFactors.WriteWindReductionArrayToCSV(options.WindDirs, options.WorkingDir, bcond, options.Mode, options.Probes, options.Verbose, out errorLogCalc);

                    errorLog.Append(errorLogCalc);


                }

            }

            else
            {
                Console.WriteLine("The licence for this tool expired.");
            }
        }
    }


    // Define a class to receive parsed values
    internal class Options
    {

        [Option('d', "workingDir", Required = true,
        HelpText = "Working directory.")]
        public string WorkingDir { get; set; }

        [Option('p', "probes", Required = true,
        HelpText = "Probes file (.pts)")]
        public string Probes { get; set; }

        [Option('w', "windDirs", Required = true,
        HelpText = "Wind directions as comma separated string - > 0,45,90")]
        public string WindDirs { get; set; }

        [Option('u', "Uref", Required = true,
        HelpText = "Reference velocity in m/s")]
        public double Uref { get; set; }

        [Option('r', "z0", Required = true,
        HelpText = "Roughness length")]
        public double Z0 { get; set; }

        [Option('z', "zref", Required = true,
        HelpText = "Reference height")]
        public double Zref { get; set; }

        [Option('m', "mode", Required = true, DefaultValue = 1,
        HelpText = "Mode: 0 = cp, 1 = U")]
        public int Mode { get; set; }

        [Option('l', "loud", DefaultValue = true,
        HelpText = "Prints all messages to standard output.")]
        public bool Verbose { get; set; }



        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }


}
