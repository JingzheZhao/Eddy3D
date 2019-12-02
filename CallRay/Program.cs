using System;
using System.IO;
using CommandLine;
using CommandLine.Text;
using EddyLib;

namespace CallRay
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            //    // Check licence

            //    if (Utilities.CheckLicence() == true)
            //    {
            //        var options = new Options();
            //        if (CommandLine.Parser.Default.ParseArguments(args, options))
            //        {
            //            string weaFileName = Path.GetFileName(options.Weather) + ".wea";
            //            Console.WriteLine(options.WorkingDir);
            //            Console.WriteLine(weaFileName);

            //            Daysim.Epw2Wea(options.Weather, options.WorkingDir + @"\Rad");

            //            DaysimSettings set = new DaysimSettings
            //            {
            //                AB = 1,
            //                WorkDir = options.WorkingDir + @"\Rad"
            //            };

            //            Daysim.RunDaysim(set);

            //            //RadianceFiles.saveILLBin(options.workingDir + @"\Rad\CallRay.dir.ill");
            //            //RadianceFiles.saveILLBin(options.workingDir + @"\Rad\CallRay.dif.ill");

            //            ////var data1 = RadianceFiles.loadILL(options.workingDir + @"\Rad\CallRay.dir.ill");
            //            ////var data2 = RadianceFiles.loadBin(options.workingDir + @"\Rad\CallRay.dir.ill.bin");

            //            Console.WriteLine("Done");

            //            // Console.ReadKey();
            //        }
            //    }
            //    else
            //    {
            //        Console.WriteLine("The licence for this tool expired.");
            //    }
        }
    }

    // Define a class to receive parsed values
    internal class Options
    {
        [Option('d', "workingDir", Required = true,
        HelpText = "Working directory.")]
        public string WorkingDir { get; set; }

        [Option('w', "weather", Required = true,
        HelpText = "EPW weather file path.")]
        public string Weather { get; set; }

        //[Option('o', "output", Required = true,
        //HelpText = "Output file path")]
        //public string output { get; set; }

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