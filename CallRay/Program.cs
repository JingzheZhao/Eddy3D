using CommandLine;
using CommandLine.Text;
using EddyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CallRay
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = new Options();
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {


                string weaFileName = Path.GetFileName(options.weather) + ".wea";
                Console.WriteLine(options.workingDir);
                Console.WriteLine(weaFileName);

                Daysim.Epw2Wea(options.weather, options.workingDir+@"\rad");

                DaysimSettings set = new DaysimSettings();

                Daysim.RunDaysim(options.workingDir + @"\rad", "bla", set);


                Console.WriteLine("Done");

                Console.ReadKey();
            }

        }
    }

    // Define a class to receive parsed values
    class Options
    {
        [Option('d', "workingDir", Required = true,
        HelpText = "Working directory.")]
        public string workingDir { get; set; }

        [Option('w', "weather", Required = true,
        HelpText = "EPW weather file path.")]
        public string weather { get; set; }


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
