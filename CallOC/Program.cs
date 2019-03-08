using CommandLine;
using CommandLine.Text;
using EddyLib;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace CallOC
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // Check licence

            if (Utilities.CheckLicence() == true)
            {
                try
                {
                    var options = new Options();
                    if (Parser.Default.ParseArguments(args, options))
                    {
                        StringBuilder errorLog = new StringBuilder();

                        int[] debug = new int[2];
#if DEBUG

                        for (int i = 0; i < 2; i++)
                        {
                            debug[i] = int.Parse(options.Hourandpoint.Split(',')[i]);
                        }

                        Console.WriteLine(@"%%% Debug mode is enabled. Make sure to pass a debug option ""hour"" and ""point"" such as -d 12,53.");

                        if (options.Hourandpoint != null)
                        {
                            Console.WriteLine("Debugging: {0}", options.Hourandpoint);
                            errorLog.AppendLine(String.Format("Debugging: {0}", options.Hourandpoint));


                        }
#endif

                        if (options.Verbose)
                        {
                            Console.WriteLine("EPW weather file path: {0}", options.Weather);
                            errorLog.AppendLine(String.Format("EPW weather file path: {0}", options.Weather));

                            Console.WriteLine("Diffuse radiation (ill): {0}", options.DifRad);
                            errorLog.AppendLine(String.Format("Diffuse radiation (ill): {0}", options.DifRad));

                            Console.WriteLine("Direct radiation (ill): {0}", options.DirRad);
                            errorLog.AppendLine(String.Format("Direct radiation (ill): {0}", options.DirRad));

                            Console.WriteLine("Wind velocity scaling factors (csv): {0}", options.WindReductionDataPath);
                            errorLog.AppendLine(String.Format("Wind velocity scaling factors (csv): {0}", options.WindReductionDataPath));

                            Console.WriteLine("Working directory: {0}", options.WorkingDir);
                            errorLog.AppendLine(String.Format("Working directory: {0}", options.WorkingDir));

                            Console.WriteLine("Wind directions: {0}", options.windDirs.ToString());
                            errorLog.AppendLine(String.Format("Wind directions: {0}", options.windDirs.ToString()));

                        }


                        bool fileMissing = false;
                        if (!Directory.Exists(options.WorkingDir)) { Console.WriteLine(options.WorkingDir + " not found. Exiting"); fileMissing = true; }
                        if (!File.Exists(options.Weather) || new FileInfo(options.Weather).Length == 0) { Console.WriteLine(options.Weather + " not found or empty. Exiting"); fileMissing = true; }
                        if (!File.Exists(options.WindReductionDataPath) || new FileInfo(options.WindReductionDataPath).Length == 0) { Console.WriteLine(options.WindReductionDataPath + " not found or empty. Exiting"); fileMissing = true; }
                        if (!File.Exists(options.DifRad) || new FileInfo(options.DifRad).Length == 0) { Console.WriteLine(options.DifRad + " not found or empty. Exiting"); fileMissing = true; }
                        if (!File.Exists(options.DirRad) || new FileInfo(options.DirRad).Length == 0) { Console.WriteLine(options.DirRad + " not found or empty. Exiting"); fileMissing = true; }
                        if (new FileInfo(options.WorkingDir + @"\Rad\sensors.pts").Length == 0) { Console.WriteLine(options.WorkingDir + @"\Rad\sensors.pts" + " not found or empty. Exiting"); fileMissing = true; }
                        if (fileMissing == true) { System.Threading.Thread.Sleep(8000); return; }



                        if (options.windDirs.Length < 8)
                        {
                            //Console.WriteLine(@"Error: You need to simulate at least 8 wind direction, preferrably ""0, 45, 90, 135, 180, 225, 270, 315"" to continue with the UTCI interpolation.");
                            errorLog.AppendLine(@"Warning: You should simulate at least 8 wind direction, preferably ""0, 45, 90, 135, 180, 225, 270, 315"" to continue with the UTCI calculation since the calculation interpolation between the simulated wind directions and the wind direction from the weather file..");
                            //throw new System.ArgumentException(@"Error: You need to simulate at least 8 wind direction, preferably ""0, 45, 90, 135, 180, 225, 270, 315"" to continue with the UTCI interpolation.");
                        }



                        Console.WriteLine("Load weather data...");

                        Weather weather = new Weather();
                        weather.LoadWeatherData(options.Weather);



                        //  Load radiation datasets
                        //  [x][]  time
                        //  [][x]  points

                        Console.WriteLine("Loading: Radiation data...");


                        var DiffRad = RadianceFiles.loadILL(options.DifRad);
                        var DirRad = RadianceFiles.loadILL(options.DirRad);



                        var numberOfHours = 8760;


                        int sensorPointCount = DiffRad[0].Length;
                        double[,] Utci = new double[numberOfHours, sensorPointCount];
                        //double[,] conditionOfPerson = new double[8760, sensorPointCount];



                       
                        Console.WriteLine("Loading: Wind data");



                        //var windDirList = new List<double> { 0, 45, 90, 135, 180, 225, 270, 315 };
                        //var windDirList = new List<double>();// { 0, 45, 90, 135, 180, 225, 270, 315 };
                        //List<int> windDirList = options.windDirs;

                        var windDirArray = options.windDirs.Split(',');
                        List<int> windDirList = new List<int>();
                        for (int i = 0; i < windDirArray.Length; i++)
                        {
                            windDirList.Add(int.Parse(windDirArray[i]));
                        }

                        var numberOfWindDirs = windDirList.Count;


                        // load Reduction data
                        // -----------------


                        var ReductionData = UTCI.LoadReductionArrayFromCSV(options.WindReductionDataPath, sensorPointCount);

                        var windReduction = UTCI.GetWindReduction(ReductionData, numberOfHours, sensorPointCount, windDirList, weather);

                        //// Importing probeHeight from probe file to scale U down to pedestrian level
                        Console.WriteLine("Parsing height of probes to scale down wind velocity from weather file.");



                        double[][] probes = EddyLib.RadianceFiles.readPTS(options.WorkingDir + @"\Rad\sensors.pts");

                        //var arbitraryProbePoint = new Point3d(probes[0][0], probes[0][1], probes[0][2]);

                        //var probingHeight = arbitraryProbePoint.Z;

                        // Parse ABL data from simulation directory                    

                        double URef = 5;
                        double zref = 10;
                        double z0 = 1;

                        try
                        {
                            var filePath = options.WorkingDir + "\\" + windDirList[0] + @"\0.org\ABLConditions";
                            if (!File.Exists(filePath)) { Console.WriteLine(filePath + " not found. Exiting"); return; }

                            Utilities.ParseABLConditionsFromCaseFolder(filePath, out URef, out z0, out zref);
                        }
                        catch (Exception e) { Console.WriteLine(e.Message); return; }



                        Console.WriteLine("Starting UTCI calc...");

                        // UTCI here

                        Stopwatch sw = new Stopwatch();

                        var uncertaintyMRTArray = new bool[numberOfHours, sensorPointCount];
                        var uncertaintyWindArray = new bool[numberOfHours, sensorPointCount];

                        UTCI.CalculateUTCIArray(probes, numberOfHours, weather, DirRad, DiffRad, windReduction, z0, zref, URef, out uncertaintyMRTArray, out uncertaintyWindArray, out sw, out Utci);

                        Console.WriteLine(Utilities.ConvertComputeTimes(sw.ElapsedMilliseconds));

                        Console.WriteLine("Writing UTCI results...");

                        UTCI.WriteUTCIDataToCSV(options.WorkingDir, probes, numberOfHours, options.Verbose, uncertaintyMRTArray, uncertaintyWindArray, Utci, debug, weather, errorLog, DiffRad, DirRad, windReduction, URef, zref, z0);

                        Console.WriteLine("Done");

                        //  Console.ReadKey();

                    }

                    else
                    {
                        // Console.WriteLine(options.GetUsage());
                        System.Threading.Thread.Sleep(5000); return;
                    }

                }






                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    System.Threading.Thread.Sleep(5000); return;
                }
            }
            else
            {
                Console.WriteLine("The licence for this tool expired.");
            }
        }
    }


}

// Define a class to receive parsed values
internal class Options
{
    [Option('w', "weather", Required = true,
    HelpText = "EPW weather file path.")]
    public string Weather { get; set; }

    [Option('f', "difRad", Required = true,
    HelpText = "Diffuse radiation (ill)")]
    public string DifRad { get; set; }

    [Option('r', "dirRad", Required = true,
    HelpText = "Direct radiation (ill)")]
    public string DirRad { get; set; }

    [Option('o', "windDirs", Required = true,
    HelpText = "List of wind directions")]
    public string windDirs { get; set; }

    [Option('u', "windReduction", Required = true,
    HelpText = "Wind velocity scaling factors (csv)")]
    public string WindReductionDataPath { get; set; }

    [Option('d', "workingDir", Required = true,
    HelpText = "Working directory.")]
    public string WorkingDir { get; set; }
    //[Option('o', "output", Required = true,
    //HelpText = "Output file path")]
    //public string output { get; set; }

    [Option('l', "loud", DefaultValue = true,
    HelpText = "Prints all messages to standard output.")]
    public bool Verbose { get; set; }

#if DEBUG
    [Option('b', "debug",
    HelpText = "Select hour and probe for debugging as comma separated string -> 23,50 meaning 23rd hour for probe 50 ")]
    public string Hourandpoint { get; set; }
#endif

    [ParserState]
    public IParserState LastParserState { get; set; }

    [HelpOption]
    public string GetUsage()
    {
        return HelpText.AutoBuild(this,
          (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
    }


}



