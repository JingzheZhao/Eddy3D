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

                        if (options.Verbose)
                        {
                            Console.WriteLine("EPW weather file path: {0}", options.Weather);
                            errorLog.AppendLine(String.Format("EPW weather file path: {0}", options.Weather));

                            Console.WriteLine("Diffuse radiation (ill): {0}", options.DifRad);
                            errorLog.AppendLine(String.Format("Diffuse radiation (ill): {0}", options.DifRad));

                            Console.WriteLine("Direct radiation (ill): {0}", options.DirRad);
                            errorLog.AppendLine(String.Format("Direct radiation (ill): {0}", options.DirRad));

                            Console.WriteLine("Wind velocity scaling factors (csv): {0}", options.WindScaling);
                            errorLog.AppendLine(String.Format("Wind velocity scaling factors (csv): {0}", options.WindScaling));

                            Console.WriteLine("Working directory: {0}", options.WorkingDir);
                            errorLog.AppendLine(String.Format("Working directory: {0}", options.WorkingDir));

                            Console.WriteLine("Wind directions: {0}", options.windDirs.ToString());
                            errorLog.AppendLine(String.Format("Wind directions: {0}", options.windDirs.ToString()));

#if DEBUG
                            if (options.Hourandpoint != null)
                            {
                                Console.WriteLine("Debugging: {0}", options.Hourandpoint);
                                errorLog.AppendLine(String.Format("Debugging: {0}", options.Hourandpoint));



                            }
#endif
                        }



                        bool fileMissing = false;
                        if (!Directory.Exists(options.WorkingDir)) { Console.WriteLine(options.WorkingDir + " not found. Exiting"); fileMissing = true; }
                        if (!File.Exists(options.Weather) || new FileInfo(options.Weather).Length == 0) { Console.WriteLine(options.Weather + " not found or empty. Exiting"); fileMissing = true; }
                        if (!File.Exists(options.WindScaling) || new FileInfo(options.WindScaling).Length == 0) { Console.WriteLine(options.WindScaling + " not found or empty. Exiting"); fileMissing = true; }
                        if (!File.Exists(options.DifRad) || new FileInfo(options.DifRad).Length == 0) { Console.WriteLine(options.DifRad + " not found or empty. Exiting"); fileMissing = true; }
                        if (!File.Exists(options.DirRad) || new FileInfo(options.DirRad).Length == 0) { Console.WriteLine(options.DirRad + " not found or empty. Exiting"); fileMissing = true; }
                        if (new FileInfo(options.WorkingDir + @"\Rad\sensors.pts").Length == 0) { Console.WriteLine(options.WorkingDir + @"\Rad\sensors.pts" + " not found or empty. Exiting"); fileMissing = true; }
                        if (fileMissing == true) { System.Threading.Thread.Sleep(8000); return; }

                        // Error checks for CFD data







                        //if (numberOfWindDirsSimulated < 8)
                        //{
                        //    //Console.WriteLine(@"Error: You need to simulate at least 8 wind direction, preferrably ""0, 45, 90, 135, 180, 225, 270, 315"" to continue with the UTCI interpolation.");
                        //    errorLog.AppendLine(@"Error: You need to simulate at least 8 wind direction, preferably ""0, 45, 90, 135, 180, 225, 270, 315"" to continue with the UTCI interpolation.");
                        //    throw new System.ArgumentException(@"Error: You need to simulate at least 8 wind direction, preferably ""0, 45, 90, 135, 180, 225, 270, 315"" to continue with the UTCI interpolation.");

                        //}








                        //  Load radiation datasets
                        //  [x][]  time
                        //  [][x]  points

                        Console.WriteLine("Load weather data...");

                        Weather weather = new Weather();
                        weather.LoadWeatherData(options.Weather);



                        Console.WriteLine("Loading: Radiation data...");


                        var DiffRad = RadianceFiles.loadILL(options.DifRad);
                        var DirRad = RadianceFiles.loadILL(options.DirRad);




                        int sensorPointCount = DiffRad[0].Length;
                        double[,] Utci = new double[8760, sensorPointCount];
                        //double[,] conditionOfPerson = new double[8760, sensorPointCount];



                        ////  Todo: implement wind scaling factor load here -- @Patrick

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


                        var ReductionData = UTCI.GetReductionData(options.WindScaling, sensorPointCount);

                        var windReduction = UTCI.GetWindReduction(ReductionData, 8760, sensorPointCount, windDirList, weather);





#if DEBUG
                        int[] debug = new int[2];

                        for (int i = 0; i < 2; i++)
                        {
                            debug[i] = int.Parse(options.Hourandpoint.Split(',')[i]);
                        }
#endif







                        //Write Reduction Array to file

                        //System.Text.StringBuilder ReductionFile = new System.Text.StringBuilder();

                        //for (int i = 0; i < numberOfWindDirs; i++)
                        //{
                        //    ReductionFile.Append(windDirList[i] + ",");

                        //}

                        //ReductionFile.AppendLine("");
                        //for (int j = 0; j < sensorPointCount; j++)
                        //{
                        //    for (int i = 0; i < 8760; i++)
                        //    {

                        //        ReductionFile.Append(String.Format("{0:0.###}",windReduction[i, j]) + ",");

                        //    }
                        //    ReductionFile.AppendLine("");
                        //}
                        //File.WriteAllText(Path.GetDirectoryName(options.windScaling) + @"\WindReductionData2.csv", ReductionFile.ToString());


                        //// Importing probeHeight from probe file to scale U down to pedestrian level
                        Console.WriteLine("Parsing height of probes to scale down wind velocity from weather file.");





                        double[][] probes = EddyLib.RadianceFiles.readPTS(options.WorkingDir + @"\Rad\sensors.pts");

                        var arbitraryProbePoint = new Point3d(probes[0][0], probes[0][1], probes[0][2]);

                        var probingHeight = arbitraryProbePoint.Z;

                        // Parse z0 from simulation directory                    

                        double URef = 5;
                        double zref = 10;
                        double z0 = 1;

                        try
                        {
                            var filePath = options.WorkingDir + "\\" + windDirList[0] + @"\0.org\ABLConditions";
                            if (!File.Exists(filePath)) { Console.WriteLine(filePath + " not found. Exiting"); return; }

                            string[] lines = File.ReadAllLines(filePath);



                            for (int i = 0; i < lines.Length; i++)
                            {
                                var l = lines[i];
                                if (l.Contains("Uref"))
                                {
                                    URef = double.Parse(l.Replace("Uref", "").Replace(";", "").Trim());
                                }

                                if (l.Contains("z0"))
                                {
                                    z0 = double.Parse(l.Replace("z0 uniform", "").Replace(";", "").Trim());
                                }

                                if (l.Contains("Zref"))
                                {
                                    zref = double.Parse(l.Replace("Zref", "").Replace(";", "").Trim());
                                }
                            }
                        }
                        catch (Exception e) { Console.WriteLine(e.Message); return; }









                        Console.WriteLine("Starting UTCI calc...");
                        Stopwatch sw = new Stopwatch(); sw.Start();



                        // UTCI here


                        int cnt = 0;
                        bool[,] uncertaintyMRTArray = new bool[8760, sensorPointCount];
                        bool[,] uncertaintyWindArray = new bool[8760, sensorPointCount];

                        using (var progress = new ASCIIProgressBar())
                        {



                            //for (int j = 0; j < sensorPointCount; j++)
                            //{

                            Parallel.For(0, sensorPointCount,
                          j =>
                          {
                              cnt++;
                              progress.Report((double)cnt / sensorPointCount);

                              for (int i = 0; i < 8760; i++)
                              {

                                  uncertaintyWindArray[i, j] = false;
                                  uncertaintyMRTArray[i, j] = false;


                                  // Check for extreme mrts

                                  double mrt = UTCI.GetMRT2(weather.DryBulbTemp[i], weather.RelativeHumidity[i], DiffRad[i][j], DirRad[i][j], weather.SolarElevation[i], weather.DryBulbTemp[i], weather.Wst, weather.Hst, weather.BodyA, weather.GrRef, 0.95)[0];

                                  if (mrt < weather.DryBulbTemp[i] - 30)
                                  {
                                      mrt = 30;
                                      uncertaintyMRTArray[i, j] = true;
                                  }
                                  if (mrt > weather.DryBulbTemp[i] + 70)
                                  {
                                      mrt = 70;
                                      uncertaintyMRTArray[i, j] = true;
                                  }


                                  // Check for extreme windspeeds

                                  double resultingWindSpeedforUTCI = windReduction[i, j] * UTCI.GetUAtProbingHeightFromEPW(weather.WindSpeed[i], z0, zref, probingHeight);

                                  if (windReduction[i, j] * UTCI.GetUAtProbingHeightFromEPW(weather.WindSpeed[i], z0, zref, probingHeight) > 17)
                                  {
                                      resultingWindSpeedforUTCI = 17;
                                      Utci[i, j] = UTCI.GetUTCI2(weather.DryBulbTemp[i], weather.RelativeHumidity[i], resultingWindSpeedforUTCI, mrt);
                                      uncertaintyWindArray[i, j] = true;
                                  }
                                  else if (resultingWindSpeedforUTCI < 0.5)
                                  {
                                      resultingWindSpeedforUTCI = 0.5;
                                      Utci[i, j] = UTCI.GetUTCI2(weather.DryBulbTemp[i], weather.RelativeHumidity[i], resultingWindSpeedforUTCI, mrt);
                                      uncertaintyWindArray[i, j] = true;
                                  }
                                  else
                                  {
                                      Utci[i, j] = UTCI.GetUTCI2(weather.DryBulbTemp[i], weather.RelativeHumidity[i], resultingWindSpeedforUTCI, mrt);
                                  }






                                  //double cOfPerson = 0;

                                  //if (Utci[i, j] < -40) cOfPerson = -5;
                                  //else if ((-40 <= Utci[i, j]) && (Utci[i, j] < -27)) cOfPerson = -4;
                                  //else if ((-27 <= Utci[i, j]) && (Utci[i, j] < -13)) cOfPerson = -3;
                                  //else if ((-13 <= Utci[i, j]) && (Utci[i, j] < 0)) cOfPerson = -2;
                                  //else if ((0 <= Utci[i, j]) && (Utci[i, j] < 9)) cOfPerson = -1;
                                  //else if ((9 <= Utci[i, j]) && (Utci[i, j] < 26)) cOfPerson = 0;
                                  //else if ((26 <= Utci[i, j]) && (Utci[i, j] < 28)) cOfPerson = 1;
                                  //else if ((28 <= Utci[i, j]) && (Utci[i, j] < 32)) cOfPerson = 2;
                                  //else if ((32 <= Utci[i, j]) && (Utci[i, j] < 38)) cOfPerson = 3;
                                  //else if ((38 <= Utci[i, j]) && (Utci[i, j] < 46)) cOfPerson = 4;
                                  //else cOfPerson = 5;


                                  //conditionOfPerson[i, j] = cOfPerson;

                              }
                              // Console.WriteLine("Sensor " + j + " done.");
                              //  }
                          });



                        }//end using prog bar

                        Console.WriteLine(Utilities.ConvertComputeTimes(sw.ElapsedMilliseconds));



                        Console.WriteLine("Writing UTCI results...");


                        //Write Array to file
                        StringBuilder sbUtci = new StringBuilder();
                        for (int j = 0; j < sensorPointCount; j++)
                        {
                            for (int i = 0; i < 8760; i++)
                            {
                                sbUtci.Append(String.Format("{0:0.0}", Utci[i, j]) + ",");
                            }
                            sbUtci.AppendLine("");
                        }
                        File.WriteAllText(options.WorkingDir + @"\UTCI.csv", sbUtci.ToString());

                        // Uncertainty output for UTCI calculations

                        StringBuilder sbUtciUncertainty = new StringBuilder();
                        sbUtciUncertainty.AppendLine("The calculated UTCI values lie outside of uncertainty (U) bounds for the following sensor points and hours either because of low/high wind velocities or MRT values:");
                        int counter = 0;
                        for (int j = 0; j < sensorPointCount; j++)
                        {

                            //Percentage for each sensorpoint
                            int cntSensorPercent = 0;
                            sbUtciUncertainty.Append("SP: " + j + ",");
                            for (int i = 0; i < 8760; i++)
                            {

                                if (uncertaintyMRTArray[i, j] == true || uncertaintyWindArray[i, j] == true)
                                {
                                    cntSensorPercent++;
                                }
                            }

                            sbUtciUncertainty.Append("\t" + (int)Math.Round((double)(100 * cntSensorPercent) / 8760) + " % U,\tHours: ");
                            cntSensorPercent = 0;
                            //Hours for each sensorpoint
                            for (int i = 0; i < 8760; i++)
                            {
                                if (uncertaintyMRTArray[i, j] == true || uncertaintyWindArray[i, j] == true)
                                {


                                    sbUtciUncertainty.Append(i + ",");
                                    counter++;
                                }
                            }
                            sbUtciUncertainty.AppendLine("");
                        }
                        sbUtciUncertainty.AppendLine("Total incidents of uncertainty: " + counter + " or " + Math.Round((double)counter * 100 / (8760 * sensorPointCount), 0) + " % overall annual uncertainty");
                        File.WriteAllText(options.WorkingDir + @"\UTCI.uncertainty", sbUtciUncertainty.ToString());


                        //Write Debug info to file
#if DEBUG
                        StringBuilder sbUtciDEBUG = new StringBuilder();

                        sbUtciDEBUG.AppendLine(@"UTCI for sensor point " + debug[1] + " over all hours of the year:");

                        for (int i = 0; i < 8760; i++)
                        {

                            sbUtciDEBUG.Append(String.Format("{0:0.0}", Utci[i, debug[1]]) + ",");
                        }
                        sbUtciDEBUG.Append(Environment.NewLine); sbUtciDEBUG.Append(Environment.NewLine);
                        sbUtciDEBUG.AppendLine("Detailed Values for sensor point " + debug[1] + " at hour " + debug[0] + ":");
                        sbUtciDEBUG.AppendLine("Air temperature: " + DryBulbTemp[debug[0]]);
                        sbUtciDEBUG.AppendLine("MRT: " + String.Format("{0:0.0}", UTCI.GetMRT2(DryBulbTemp[debug[0]], RelativeHumidity[debug[0]], DiffRad[debug[0]][debug[1]], DirRad[debug[0]][debug[1]], SolarElevation[debug[0]], DryBulbTemp[debug[0]], Wst, Hst, BodyA, GrRef, 0.95)[0]));
                        sbUtciDEBUG.AppendLine("Vapour pressure: " + Pressure[debug[0]]);
                        sbUtciDEBUG.AppendLine("Relative humidity: " + RelativeHumidity[debug[0]]);


                        sbUtciDEBUG.AppendLine("Wind speed from .epw: " + String.Format("{0:0.0}", WindSpeed[debug[0]]));
                        sbUtciDEBUG.AppendLine("probingHeight from CFD: " + String.Format("{0:0.0}", probingHeight));
                        sbUtciDEBUG.AppendLine("Scaled-down wind velocity from .epw: " + String.Format("{0:0.0}", UTCI.GetUAtProbingHeightFromEPW(WindSpeed[debug[0]], z0, zref, probingHeight)));
                        sbUtciDEBUG.AppendLine("Wind reduction from CFD: " + String.Format("{0:0.0}", windReduction[debug[0], debug[1]]));
                        sbUtciDEBUG.AppendLine("Resulting wind velocity for UTCI calculation: " + String.Format("{0:0.0}", windReduction[debug[0], debug[1]] * UTCI.GetUAtProbingHeightFromEPW(WindSpeed[debug[0]], z0, zref, probingHeight)));

                        sbUtciDEBUG.AppendLine("UTCI: " + String.Format("{0:0.0}", Utci[debug[0], debug[1]]));
                        sbUtciDEBUG.AppendLine("");
                        File.WriteAllText(options.WorkingDir + @"\UTCI_debug_hour_" + debug[0] + "_probe_" + debug[1] + ".csv", sbUtciDEBUG.ToString());
#endif



                        if (options.Verbose)
                        {
                            File.WriteAllText(options.WorkingDir + @"\UTCI.err", errorLog.ToString());
                        }

                        Console.WriteLine("Done");

                        //  Console.ReadKey();




                    }

                    else
                    {
                        // Console.WriteLine(options.GetUsage());
                        System.Threading.Thread.Sleep(5000); return;
                    }







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

private static void system(string v)
{
    throw new NotImplementedException();
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

    [Option('u', "windScaling", Required = true,
    HelpText = "Wind velocity scaling factors (csv)")]
    public string WindScaling { get; set; }

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


}
