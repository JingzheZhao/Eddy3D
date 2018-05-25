using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CallBatchRunner
{
    class Program
    {
        static void Main(string[] args)
        {

            var options = new Options();
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {


                var batchFiles = Directory.GetFiles(options.workingDir, options.pattern , SearchOption.AllDirectories);  //"*.bat"

                //multithreaded with limit
                CancellationToken ct = new CancellationToken();
                Stopwatch stopw = new Stopwatch();
                stopw.Start();
                var parallelOptions = new ParallelOptions()
                {
                    MaxDegreeOfParallelism = options.threads,
                    CancellationToken = ct
                };
                Parallel.For(0, batchFiles.Length, parallelOptions, (i) =>
                {

                    // launch procs here...
                    Console.WriteLine(batchFiles[i]);


                    var processInfo = new ProcessStartInfo("cmd.exe", "/c"  +"\"" +batchFiles[i] + "\"") ;
                    processInfo.CreateNoWindow = true;
                    processInfo.UseShellExecute = false;
                    processInfo.RedirectStandardError = true;
                    processInfo.RedirectStandardOutput = true;

                    var process = Process.Start(processInfo);

                    process.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                        Console.WriteLine("output>>" + e.Data);
                    process.BeginOutputReadLine();

                    process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
                        Console.WriteLine("error>>" + e.Data);
                    process.BeginErrorReadLine();

                    process.WaitForExit();

                    Console.WriteLine("ExitCode: {0}", process.ExitCode);
                    process.Close();

                }
                );
                stopw.Stop();
                Console.WriteLine("Batch runner complete: " + stopw.Elapsed);



                Console.ReadKey();

            }

        }
    }
        // Define a class to receive parsed values
        class Options
        {
            [Option('w', "workingDir", Required = true,
            HelpText = "Working directory.")]
            public string workingDir { get; set; }

            [Option('t', "threads", Required = true, DefaultValue = 4,
            HelpText = "Number of parallel threads")]
            public int threads { get; set; }


        [Option('p', "pattern", Required = true, DefaultValue = "*.bat",
HelpText = "File search pattern")]
        public string pattern { get; set; }


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
