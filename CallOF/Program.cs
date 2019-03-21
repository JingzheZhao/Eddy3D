using CommandLine;
using CommandLine.Text;
using SlavaGu.ConsoleAppLauncher;
using System;
using System.Text;

namespace CallOF
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var options = new Options();
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                StringBuilder errorLog = new StringBuilder();

                //if (options.CPUs == -1 || options.CPUs > Environment.ProcessorCount)
                //{
                //    options.CPUs = Environment.ProcessorCount;
                //}
                // Values are available here
                if (options.Verbose)
                {
                    Console.WriteLine("File path: {0}", options.FilePath);
                    //Console.WriteLine("Output: {0}", options.OutputFile);
                    Console.WriteLine("Executable: {0}", options.Command);
                    //Console.WriteLine("Viscosity: {0}", options.Visc);
                    //Console.WriteLine("Processors used: {0}", options.CPUs);
                    //Console.WriteLine("Abort if error smaller than: {0}", options.MaxErr);

                    errorLog.AppendLine(String.Format("File path: {0}", options.FilePath));
                    //errorLog.AppendLine(String.Format("Output: {0}", options.OutputFile));
                    errorLog.AppendLine(String.Format("Executable: {0}", options.Command));
                    //errorLog.AppendLine(String.Format("Viscosity: {0}", options.Visc));
                    //errorLog.AppendLine(String.Format("Processors used: {0}", options.CPUs));
                    //errorLog.AppendLine(String.Format("Abort if error smaller than: {0}", options.MaxErr));
                }





                string app = "docker";
                //string filepath = "/c/OF/";
                string volumeDocker = "/home/openfoam/";
                string entryPoint = @"-i --entrypoint=""""";  // -it didnt work --> the input device is not a TTY.  If you are using mintty, try prefixing the command with 'winpty'
                string container = "hfdresearch/swak4foamandpyfoam:latest-v4.1 ";
                string sourceEnvironment = @"source /opt/openfoam4/etc/bashrc; cd /home/openfoam; ";
                string logging = @"";
                //string command = "blockMesh";
                var app_argument = string.Format("run -v \"{0}:{1}\" {2} {3} bash -c \"{4}{5}{6}\"", options.FilePath.Trim(), volumeDocker, entryPoint, container, sourceEnvironment, options.Command, logging);
                //Environment.SetEnvironmentVariable("PATH", @"C:\Program Files\Docker\Docker\Resources\bin");


                //Show stdout after executed command

                //Console.WriteLine(ConsoleApp.Run(app, app_argument).Output.Trim());

                //Don't show stdout asynchronosly


                try
                {
                    var p = new ConsoleApp(app, app_argument);
                    p.ConsoleOutput += (o, args1) =>
                    {
                        Console.WriteLine(args1.Line);
                    };
                    p.Run();

                    p.WaitForExit();
                    p.Stop();


#if DEBUG

                    Console.ReadKey();

#endif
                }
                catch (Exception e)
                {

                    { Console.WriteLine(e.Message); };


                }




            }


        }
    }


    // Define a class to receive parsed values
    internal class Options
    {
        [Option('f', "filePath", Required = true,
        HelpText = "File path.")]
        public string FilePath { get; set; }

        //[Option('o', "output", Required = true,
        //HelpText = "Output file to be generated.")]
        //public string OutputFile { get; set; }

        //[Option('e', "executable", Required = true,
        //HelpText = "Binary to be executed.")]
        //public string command { get; set; }

        [Option('e', "executable", Required = true,
        HelpText = "Binary to be executed.")]
        public string Command { get; set; }

        //[Option('p', "parallel processes", DefaultValue = -1,
        //HelpText = "Number of parallel processes allowed.")]
        //public int CPUs { get; set; }

        //[Option('e', "abortion criterion", DefaultValue = 1e-4,
        //HelpText = "Relative error monitored at probes as abortion criterion.")]
        //public double MaxErr { get; set; }

        //[Option('v', "viscosity", DefaultValue = 0.015,
        //HelpText = "Sets the viscosity of the fluid.")]
        //public double Visc { get; set; }

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