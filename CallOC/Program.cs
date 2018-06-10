using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using EddyLib;
using System.Diagnostics;
using Rhino.Geometry;

namespace CallOC
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var options = new Options();
                if (Parser.Default.ParseArguments(args, options))
                {
                    StringBuilder errorLog = new StringBuilder();

                    if (options.Verbose)
                    {
                        Console.WriteLine("EPW weather file path: {0}", options.weather);
                        errorLog.AppendLine(String.Format("EPW weather file path: {0}", options.weather));

                        Console.WriteLine("Diffuse radiation (ill): {0}", options.difRad);
                        errorLog.AppendLine(String.Format("Diffuse radiation (ill): {0}", options.difRad));

                        Console.WriteLine("Direct radiation (ill): {0}", options.dirRad);
                        errorLog.AppendLine(String.Format("Direct radiation (ill): {0}", options.dirRad));

                        Console.WriteLine("Wind speed scaling factors (csv): {0}", options.windScaling);
                        errorLog.AppendLine(String.Format("Wind speed scaling factors (csv): {0}", options.windScaling));

                        Console.WriteLine("Working directory: {0}", options.workingDir);
                        errorLog.AppendLine(String.Format("Working directory: {0}", options.workingDir));

                        if (options.hourandpoint != null)
                        {
                            Console.WriteLine("Debugging: {0}", options.hourandpoint);
                            errorLog.AppendLine(String.Format("Debugging: {0}", options.hourandpoint));
                        }
                        
                    }


                    bool fileMissing = false;
                    if (!Directory.Exists(options.workingDir)) { Console.WriteLine(options.workingDir + " not found. Exiting"); fileMissing = true; }
                    if (!File.Exists(options.weather)) { Console.WriteLine(options.weather + " not found. Exiting"); fileMissing = true; }
                    if (!File.Exists(options.windScaling)) { Console.WriteLine(options.windScaling + " not found. Exiting"); fileMissing = true; }
                    if (!File.Exists(options.difRad)) { Console.WriteLine(options.difRad + " not found. Exiting"); fileMissing = true; }
                    if (!File.Exists(options.dirRad)) { Console.WriteLine(options.difRad + " not found. Exiting"); fileMissing = true; }

                    if (fileMissing == true) { System.Threading.Thread.Sleep(8000); return; }

                    // Error checks for CFD data

                    // load  data
                    // -----------------
                    var ReductionData = File.ReadAllLines(options.windScaling).Skip(1).ToArray();

                    var numberOfWindDirsSimulated = ReductionData[0].Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Count();

                    if (numberOfWindDirsSimulated < 8)
                    {
                        //Console.WriteLine(@"Error: You need to simulate at least 8 wind direction, preferrably ""0, 45, 90, 135, 180, 225, 270, 315"" to continue with the UTCI interpolation.");
                        throw new System.ArgumentException(@"Error: You need to simulate at least 8 wind direction, preferrably ""0, 45, 90, 135, 180, 225, 270, 315"" to continue with the UTCI interpolation.");
                    }




                    // load weather data
                    // -----------------
                    string[] epwData = File.ReadAllLines(options.weather);

                    // get header data
                    string[] ln1 = epwData[0].Split(',');

                    var Location = System.Text.RegularExpressions.Regex.Replace((ln1[1]), @"\s+", "");
                    var Latitude = Double.Parse(ln1[6]);
                    var Longitude = Double.Parse(ln1[7]);
                    var TimeZone = Double.Parse(ln1[8]);

                    // get hourly data
                    string[] epwNoHeader = epwData.Skip(8).Take(8760).ToArray(); // new ArraySegment<string>(epwData, 8, 8760).Array;//.ToArray();

                    var DryBulbTemp = epwNoHeader.Select(o => Double.Parse(o.Split(',')[6])).ToArray(); // Dry Bulb Temperature
                    var DewPointTemp = epwNoHeader.Select(o => Double.Parse(o.Split(',')[7])).ToArray(); // Dew Point Temperature
                    var RelativeHumidity = epwNoHeader.Select(o => Double.Parse(o.Split(',')[8])).ToArray(); // Relative Humidity
                    var Pressure = epwNoHeader.Select(o => Double.Parse(o.Split(',')[9])).ToArray(); // Barometric Pressure
                    var WindSpeed = epwNoHeader.Select(o => Double.Parse(o.Split(',')[21])).ToArray(); // WindSpeed
                    var WindDirection = epwNoHeader.Select(o => Double.Parse(o.Split(',')[20])).ToArray(); // Wind Direction
                    var DirectNormalRadiation = epwNoHeader.Select(o => Double.Parse(o.Split(',')[14])).ToArray(); // Direct Normal Radiation
                    var DiffuseHorizontalRadiation = epwNoHeader.Select(o => Double.Parse(o.Split(',')[15])).ToArray(); // Diffuse Horizontal Illuminance
                                                                                                                        //var GlobalHorizontalRadiation = epwNoHeader.Select(o => Double.Parse(o.Split(',')[13])); // Global Horizontal Illuminance
                                                                                                                        //var SkyCover = epwNoHeader.Select(o => Double.Parse(o.Split(',')[22])); // Global Horizontal Illuminance

                    var Yr = epwNoHeader.Select(o => Double.Parse(o.Split(',')[0])).ToArray();
                    var Mo = epwNoHeader.Select(o => Double.Parse(o.Split(',')[1])).ToArray();
                    var Dy = epwNoHeader.Select(o => Double.Parse(o.Split(',')[2])).ToArray();
                    var Hr = epwNoHeader.Select(o => Double.Parse(o.Split(',')[3])).ToArray();
                    var DateTime = epwNoHeader.Select(o => o.Split(',')[0] + "." + o.Split(',')[1] + "." + o.Split(',')[2] + " " + o.Split(',')[3]).ToArray();

                    Console.WriteLine("Calculating: Solar Geometry");
                    var sg = new SolarGeometry();

                    var SolarElevation = new List<double>();
                    var SolarAzi = new List<double>();
                    for (int i = 0; i < Yr.Length; i++)
                    {
                        double _el = sg.solarelevation(Latitude, Longitude, Yr[i], Mo[i], Dy[i], Hr[i], 0, 0, TimeZone, 0);
                        double _az = sg.solarazimuth(Latitude, Longitude, Yr[i], Mo[i], Dy[i], Hr[i], 0, 0, TimeZone, 0);

                        if (_el > 0)
                        {
                            SolarElevation.Add(_el);
                            SolarAzi.Add(_az);
                        }
                        else
                        {
                            SolarElevation.Add(0);
                            SolarAzi.Add(0);
                        }

                    }

                    // constants that should be dealt with later
                    //-----------------------

                    double Wst, Hst, BodyA, GrRef;
                    Wst = 30;
                    Hst = 30;
                    BodyA = 0.5;
                    GrRef = 0.2;

                    //  Load radiation datasets
                    //  [x][]  time
                    //  [][x]  points
                    Console.WriteLine("Loading: Radiation data");


                    var DiffRad = RadianceFiles.loadILL(options.difRad);
                    var DirRad = RadianceFiles.loadILL(options.dirRad);




                    int sensorPointCount = DiffRad[0].Length;
                    double[,] Utci = new double[8760, sensorPointCount];
                    double[,] conditionOfPerson = new double[8760, sensorPointCount];



                    ////  Todo: implement wind scaling factor load here -- @Patrick

                    Console.WriteLine("Loading: Wind data");

                    var windDirList = new List<double> { 0, 45, 90, 135, 180, 225, 270, 315 };

                    var numberOfWindDirs = windDirList.Count;

                    int[] debug = new int[2];


                    for (int i = 0; i < 2; i++)
                    {
                        debug[i] = int.Parse(options.hourandpoint.Split(',')[i]);
                    }




                    for (int i = 0; i < sensorPointCount; i++)
                    {
                        var l = ReductionData[i];
                        if (l.Contains("∞")) ReductionData[i] = l.Replace("∞", "0");
                    }





                    // Array of Reduction data

                    double[,] windReduction = new double[8760, sensorPointCount];

                    int cntReduction = 0;
                    using (var progress = new ASCIIProgressBar())
                    {


                        var ReductionArray = new double[numberOfWindDirs][];

                        for (int d = 0; d < numberOfWindDirs; d++)
                        {

                            ReductionArray[d] = new double[sensorPointCount];
                            for (int p = 0; p < sensorPointCount; p++)
                            {
                                ReductionArray[d][p] = double.Parse(ReductionData[p].Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[d]);
                            }
                        }

                        Console.WriteLine("Calculating: Wind reduction factors");

                        for (int j = 0; j < sensorPointCount; j++)
                        {
                            cntReduction++;
                            progress.Report((double)cntReduction / sensorPointCount);
                            for (int i = 0; i < 8760; i++)
                            {

                                // hours of weather file in iterator missing
                                windReduction[i, j] = UTCI.GetWindReductionFactor(j, ReductionArray, sensorPointCount, windDirList, WindSpeed[i], WindDirection[i]);
                            }
                        }

                    }

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


                    ////

                    Console.WriteLine("Starting UTCI calc...");
                    Stopwatch sw = new Stopwatch(); sw.Start();

                    int cnt = 0;

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

                              double mrt = UTCI.GetMRT2(DryBulbTemp[i], RelativeHumidity[i], DiffRad[i][j], DirRad[i][j], SolarElevation[i], DryBulbTemp[i], Wst, Hst, BodyA, GrRef, 0.95)[0];

                              double utci_temp = UTCI.GetUTCI2(DryBulbTemp[i], RelativeHumidity[i], windReduction[i, j] * WindSpeed[i], mrt);
                              Utci[i, j] = utci_temp;

                              //double cOfPerson = 0;

                              //if (utci_temp < -40) cOfPerson = -5;
                              //else if ((-40 <= utci_temp) && (utci_temp < -27)) cOfPerson = -4;
                              //else if ((-27 <= utci_temp) && (utci_temp < -13)) cOfPerson = -3;
                              //else if ((-13 <= utci_temp) && (utci_temp < 0)) cOfPerson = -2;
                              //else if ((0 <= utci_temp) && (utci_temp < 9)) cOfPerson = -1;
                              //else if ((9 <= utci_temp) && (utci_temp < 26)) cOfPerson = 0;
                              //else if ((26 <= utci_temp) && (utci_temp < 28)) cOfPerson = 1;
                              //else if ((28 <= utci_temp) && (utci_temp < 32)) cOfPerson = 2;
                              //else if ((32 <= utci_temp) && (utci_temp < 38)) cOfPerson = 3;
                              //else if ((38 <= utci_temp) && (utci_temp < 46)) cOfPerson = 4;
                              //else cOfPerson = 5;


                              //conditionOfPerson[i, j] = cOfPerson;

                          }
                          // Console.WriteLine("Sensor " + j + " done.");
                          //  }
                      });



                    }//end using prog bar

                    if (sw.ElapsedMilliseconds < 60 * 1000)
                    {
                        Console.WriteLine("Compute time: " + sw.ElapsedMilliseconds / 1000 + " s");
                    }

                    else
                    {
                        Console.WriteLine("Compute time: " + sw.ElapsedMilliseconds / 1000 + " s or ca. " + sw.ElapsedMilliseconds / 1000 / 60 + " min");
                    }



                    Console.WriteLine("Writing UTCI results...");


                    //Write Array to file
                    StringBuilder sbUtci = new StringBuilder();
                    for (int j = 0; j < sensorPointCount; j++)
                    {
                        for (int i = 0; i < 8760; i++)
                        {
                            sbUtci.Append(String.Format("{0:0.##}", Utci[i, j]) + ",");
                        }
                        sbUtci.AppendLine("");
                    }
                    File.WriteAllText(options.workingDir + @"\UTCI.csv", sbUtci.ToString());


#if DEBUG
                    //Write Debug info to file
                    StringBuilder sbUtciDEBUG = new StringBuilder();

                    sbUtciDEBUG.AppendLine(@"UTCI for sensor point " + debug[1] + " over all hours of the year.");

                    for (int i = 0; i < 8760; i++)
                    {

                        sbUtciDEBUG.Append(String.Format("{0:0}", Utci[i, debug[1]]) + ",");
                    }
                    sbUtciDEBUG.AppendLine("Detailed Values for sensor point " + debug[1] + " at hour " + debug[0] + ":");
                    sbUtciDEBUG.AppendLine("Air temperature: " + DryBulbTemp[debug[0]]);
                    sbUtciDEBUG.AppendLine("MRT: " + UTCI.GetMRT2(DryBulbTemp[debug[0]], RelativeHumidity[debug[0]], DiffRad[debug[0]][0], DirRad[debug[0]][0], SolarElevation[debug[0]], DryBulbTemp[debug[0]], Wst, Hst, BodyA, GrRef, 0.95)[0]);
                    sbUtciDEBUG.AppendLine("Vapour pressure: " + Pressure[debug[0]]);
                    sbUtciDEBUG.AppendLine("Relative humidity: " + RelativeHumidity[debug[0]]);
                    sbUtciDEBUG.AppendLine("Wind reduction: " + windReduction[debug[0], debug[1]]);

                    sbUtciDEBUG.AppendLine("Wind speed: " + WindSpeed[debug[0]]);

                    sbUtciDEBUG.AppendLine("UTCI: " + String.Format("{0:0.##}", Utci[debug[0], debug[1]]));
                    sbUtciDEBUG.AppendLine("");
                    File.WriteAllText(options.workingDir + @"\UTCI_debug.csv", sbUtciDEBUG.ToString());


#endif



                    if (options.Verbose)
                    {
                        File.WriteAllText(options.workingDir + @"\UTCI.err", errorLog.ToString());
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

            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                System.Threading.Thread.Sleep(5000); return;
            }
        }
    }

    // Define a class to receive parsed values
    class Options
    {
        [Option('w', "weather", Required = true,
        HelpText = "EPW weather file path.")]
        public string weather { get; set; }

        [Option('f', "difRad", Required = true,
        HelpText = "Diffuse radiation (ill)")]
        public string difRad { get; set; }

        [Option('r', "dirRad", Required = true,
        HelpText = "Direct radiation (ill)")]
        public string dirRad { get; set; }


        [Option('u', "windScaling", Required = true,
        HelpText = "Wind speed scaling factors (csv)")]
        public string windScaling { get; set; }

        [Option('d', "workingDir", Required = true,
                HelpText = "Working directory.")]
        public string workingDir { get; set; }
        //[Option('o', "output", Required = true,
        //HelpText = "Output file path")]
        //public string output { get; set; }

        [Option('l', "loud", DefaultValue = true,
        HelpText = "Prints all messages to standard output.")]
        public bool Verbose { get; set; }

        [Option('b', "debug",
        HelpText = "Select hour and probe for debugging as comma separated string -> 23,50 meaning 23rd hour for probe 50 ")]
        public string hourandpoint { get; set; }


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
