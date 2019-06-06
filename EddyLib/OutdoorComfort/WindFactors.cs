using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EddyLib
{
    public static class WindFactors
    {
        public static object Options { get; private set; }


        public static double GetVelocityAtProbingHeightFromEPW(double URef, double z0, double zref, double probingHeight)
        {
            var UAtProbingHeightFromEPW = ((0.41 * URef) / Math.Log((zref + z0) / z0) / 0.41) * Math.Log((probingHeight + z0) / z0);
            return UAtProbingHeightFromEPW;
        }
                

        public static string[] LoadWindReductionArrayFromCSV(string filePath)
        {
            var ReductionData = File.ReadAllLines(filePath).Skip(1).ToArray();
            int sensorPointCount = ReductionData.Length;

            var numberOfWindDirsSimulated = ReductionData[0].Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Count();

            for (int i = 0; i < sensorPointCount; i++)
            {
                var l = ReductionData[i];
                if (l.Contains("∞"))
                {
                    ReductionData[i] = l.Replace("∞", "0");
                }
            }
            return ReductionData;
        }

        public static double[,] GetWindReduction(string[] ReductionData, int numberOfHours, List<int> simulatedWindDirList, Weather weather)
        {

            int sensorPointCount = ReductionData.Length;
            int numberOfWindDirs = simulatedWindDirList.Count;

            // Array of Reduction data

            double[,] windReduction = new double[numberOfHours, sensorPointCount];

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
                    for (int i = 0; i < numberOfHours; i++)
                    {

                        // hours of weather file in iterator missing
                        windReduction[i, j] = WindFactors.GetWindReductionFactor(j, ReductionArray, sensorPointCount, simulatedWindDirList, weather.WindSpeed[i], weather.WindDirection[i]);
                    }
                }

            }
            return windReduction;
        }

        public static void WriteWindReductionArrayToCSV(string WindDirs, string WorkingDir, int Mode, double URef, double zref, double z0, string probesFilePath, bool Verbose, out StringBuilder errorLog)
        {
            errorLog = new StringBuilder();
            errorLog.AppendLine("test");

            var simulatedWindDirList = WindDirs.Split(',');
            int numberOfWindDirs = simulatedWindDirList.Length;

            // Read all variables from one file path. Variables are usually identical for all wind directions so this should be robust.
            var ABLfilePath = WorkingDir + "\\" + WindDirs.Split(',')[0] + @"\0.org\ABLConditions";

            try
            {

                // Error checking

                if (!Directory.Exists(WorkingDir)) { errorLog.AppendLine(WorkingDir + " not found. Exiting"); Console.WriteLine(WorkingDir + " not found. Exiting"); }

                if (Utilities.IsDirectoryEmpty(WorkingDir + @"\mesh\constant\polyMesh"))
                {
                    errorLog.AppendLine("The mesh folder is empty. Can't pull probes from a mesh that does not exist.");
                    //throw new System.ArgumentException("The mesh folder is empty. Can't pull probes from a mesh that does not exist.");
                }


                for (int i = 0; i < numberOfWindDirs; i++)
                {
                    var fp = WorkingDir + @"\" + simulatedWindDirList[i] + @"\system\U_Probes";
                    if (!File.Exists(fp))
                    {
                        errorLog.AppendLine(@"The wind direction """ + simulatedWindDirList[i] + @""" misses the probing dictionary. Please connect the ""writeProbes"" component and recompute the solution.");
                        throw new System.ArgumentException("The wind direction " + simulatedWindDirList[i] + @" misses the probing dictionary. Please connect the component ""writeProbes"" and recompute the solution.");
                    }
                }

                for (int i = 0; i < numberOfWindDirs; i++)
                {
                    var fp = WorkingDir + @"\" + simulatedWindDirList[i] + @"\constant\polyMesh";
                    if (!Directory.Exists(fp))
                    {
                        errorLog.AppendLine(@"The wind direction """ + simulatedWindDirList[i] + @""" misses the ""\constant\polyMesh"" dictionary. Please make sure that directory exists.");
                        throw new System.ArgumentException("The wind direction " + simulatedWindDirList[i] + @" misses the ""\constant\polyMesh"" dictionary. Please make sure that directory exists.");
                    }
                }

                for (int i = 0; i < numberOfWindDirs; i++)
                {
                    ABLfilePath = WorkingDir + "\\" + simulatedWindDirList[i] + @"\0.org\ABLConditions";
                    if (!File.Exists(ABLfilePath)) { Console.WriteLine(ABLfilePath + " not found. Exiting"); errorLog.AppendLine(ABLfilePath + " not found. Exiting"); }
                }


                // Check if U file is in last iteration
                for (int i = 0; i < numberOfWindDirs; i++)
                {
                    string iter = Utilities.GetLastIterationFromDirectory(WorkingDir + @"\" + simulatedWindDirList[i]).ToString();
                    string fp = WorkingDir + @"\" + simulatedWindDirList[i] + @"\" + iter + @"\U";


                    if (!File.Exists(fp))
                    {
                        errorLog.AppendLine(@"The simulation folder of the wind direction """ + simulatedWindDirList[i] + @""" misses the velocity (U) result file. Please make sure that U is calculated for this particular timestep (change WriteInterval) and recompute the solution.");
                        throw new System.ArgumentException(@"The simulation folder of the wind direction """ + simulatedWindDirList[i] + @""" misses the velocity (U) result file. Please make sure that U is calculated for this particular timestep (change WriteInterval) and recompute the solution.");
                    }
                }





                // Delete files in subfolders
                var listOfDirsInfo = new List<string>();

                for (int i = 0; i < numberOfWindDirs; i++)
                {
                    listOfDirsInfo.Add((@"C:\Temp\" + simulatedWindDirList[i] + @"\postProcessing\"));

                }



                //for (int i = 0; i < numberOfWindDirs; i++)
                //{


                //    foreach (var subDir in new DirectoryInfo(listOfDirsInfo[i]).GetDirectories())
                //    {

                //        if (subDir.ToString().ToLower() == "residuals")
                //        {
                //            continue;
                //        }
                //        subDir.Delete(true);
                //    }
                //}



                Utilities.ParseABLConditionsFromCaseFolder(ABLfilePath, out URef, out z0, out zref);

                double[][] probes = EddyLib.RadianceFiles.readPTS(probesFilePath);
                var numberOfProbes = probes.GetLength(0);

                List<Point3d> pointList = new List<Point3d>();

                for (int i = 0; i < probes.GetLength(0); i++)
                {
                    pointList.Add(new Point3d(probes[i][0], probes[i][1], probes[i][2]));
                }



                if (Mode == 0) // cp
                {

                    //try
                    //{


                    //    StringBuilder command = new StringBuilder();

                    //    string pointName = "cp_Probes";
                    //    string OFfield = "total(p)_coeff";

                    //    for (int i = 0; i < numberOfWindDirs; i++)
                    //    {

                    //        //File.WriteAllText(options.workingDir + dirs[i] + @"\system\" + "controlDict", EddyLib.StringTemplatescontrolDict(DOM, null, i));
                    //        //File.WriteAllText(options.workingDir + dirs[i] + @"\system\" + pointName, EddyLib.StringTemplatessampleProbes(listOfPoints, pointName, options.mode));
                    //        command.Append(@"postProcess -case " + windDirs[i] + " -func " + pointName + @" -newTimes | tee  " + windDirs[i] + @"/log_probes;");



                    //    }

                    //    ProcessStartInfo psi = new ProcessStartInfo(EddyLib.Utilities.AssemblyDirectory + @"\CallOF.exe", @" -e """ + command + @""" -f " + "\"" + options.workingDir);
                    //    Process p = new Process();
                    //    p.StartInfo = psi;
                    //    p.Start();
                    //    p.WaitForExit();
                    //    //p.Close();

                    //    Thread.Sleep(2 * probes.GetLength(0) * numberOfWindDirs);


                    //    for (int i = 0; i < numberOfWindDirs; i++)
                    //    {
                    //        //Thread.Sleep(2 * probes.GetLength(0));
                    //        ParsingProbes cp = new ParsingProbes(pointList, pointName, options.workingDir + "\\" + windDirs[i], OFfield);
                    //        //cpTree.AddRange(cp.cpValues, new Grasshopper.Kernel.Data.GH_Path(i));
                    //    }

                    //}
                    //catch (Exception e) { Console.WriteLine(e.Message); return; }


                }

                if (Mode == 1) // U
                {

                    Console.WriteLine("Probing the simulation results.");

                    Stopwatch sw = new Stopwatch(); sw.Start();



                    StringBuilder command = new StringBuilder();

                    string pointName = "U_Probes";
                    string OFfield = "U";

                    for (int i = 0; i < numberOfWindDirs; i++)
                    {

                        // Write the dicts

                        //File.WriteAllText(options.workingDir + dirs[i] + @"\system\" + pointName, EddyLib.StringTemplatessampleProbes(listOfPoints, pointName, options.mode));
                        command.Append(@"postProcess -case " + simulatedWindDirList[i] + " -func " + pointName + @" -latestTime | tee -a  " + simulatedWindDirList[i] + @"/log_probes;");


                    }


                    ProcessStartInfo psi = new ProcessStartInfo(Utilities.AssemblyDirectory + @"\CallOF.exe", @" -e """ + command + @""" -f " + "\"" + WorkingDir);
                    Process p = new Process
                    {
                        StartInfo = psi
                    };
                    p.Start();
                    p.WaitForExit();
                    p.Close();

                    // Issue
                    // Could not find a part of the path 'C:\temp\0\PostProcessing\U_Probes'.
                    // This happens if OF process closes immideately after calling

                    //Thread.Sleep(2 * 30* Math.Sqrt(probes.GetLength(0)) * numberOfWindDirs);

                    Console.WriteLine(Utilities.ConvertComputeTimes(sw.ElapsedMilliseconds));



                    Console.WriteLine("Parsing the velocity vectors for the probes of every wind direction and writing result files.");
                    Stopwatch sw2 = new Stopwatch(); sw2.Start();


                    for (int i = 0; i < numberOfWindDirs; i++)
                    {

                        // Parse values
                        //Thread.Sleep(2 * probes.GetLength(0));

                        var ofField = new OFField(OFfield, pointName);
                        var U = new Probing(pointList, WorkingDir + "\\" + simulatedWindDirList[i], ofField);

                        // Create datatree

                        // uTree.AddRange(U.uValues, new Grasshopper.Kernel.Data.GH_Path(i));

                    }


                    List<string> fullProbeFilePath = new List<String>();

                    //var numberOfProbes = File.ReadAllLines(fullProbeFilePath[0]).Count(); //defined above
                    //string[] abc = replacedString.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);


                    //Build list of paths



                    for (int i = 0; i < numberOfWindDirs; i++)
                    {
                        var path = WorkingDir + "\\" + simulatedWindDirList[i] + @"\postProcessing\U_Probes.csv";
                        if (!File.Exists(path)) { Console.WriteLine(path + " not found. Exiting"); errorLog.AppendLine(path + " not found. Exiting"); return; }
                        fullProbeFilePath.Add(path);
                    }


                    Console.WriteLine(Utilities.ConvertComputeTimes(sw2.ElapsedMilliseconds));




                    // Array for output data

                    Console.WriteLine("Re-collecting output data from every wind direction.");
                    Stopwatch sw3 = new Stopwatch(); sw3.Start();


                    Vector3d[,] AnnualData = new Vector3d[numberOfWindDirs, numberOfProbes];

                    var UData = new string[numberOfWindDirs][];

                    for (int i = 0; i < numberOfWindDirs; i++)
                    {
                        UData[i] = File.ReadAllLines(fullProbeFilePath[i]);
                    }


                    //UData[0] = File.ReadAllLines(fullProbeFilePath[0]);
                    //UData[1] = File.ReadAllLines(fullProbeFilePath[1]);
                    //UData[2] = File.ReadAllLines(fullProbeFilePath[2]);
                    //UData[3] = File.ReadAllLines(fullProbeFilePath[3]);
                    //UData[4] = File.ReadAllLines(fullProbeFilePath[4]);
                    //UData[5] = File.ReadAllLines(fullProbeFilePath[5]);
                    //UData[6] = File.ReadAllLines(fullProbeFilePath[6]);
                    //UData[7] = File.ReadAllLines(fullProbeFilePath[7]);

                    using (var progress = new ASCIIProgressBar())
                    {
                        int cnt = 0;
                        Parallel.For(0, numberOfWindDirs,
                        r =>

                        {

                                    //for (int r = 0; r < numberOfWindDirs; r++)
                                    //{
                                    //listOfAnnualData[r] = new Vector3d[numberOfProbes];
                                    for (int c = 0; c < numberOfProbes; c++)
                            {
                                AnnualData[r, c] = new Vector3d(double.Parse(UData[r][c].Split(',')[0]), double.Parse(UData[r][c].Split(',')[1]), double.Parse(UData[r][c].Split(',')[2]));
                                progress.Report((double)cnt / numberOfProbes * numberOfWindDirs);
                                cnt++;
                            }
                                    //}
                                });
                    }

                    Console.WriteLine(Utilities.ConvertComputeTimes(sw3.ElapsedMilliseconds));


                    //Write U Array to file
                    Console.WriteLine("Writing U Array");
                    Stopwatch sw4 = new Stopwatch(); sw4.Start();


                    System.Text.StringBuilder UFile = new System.Text.StringBuilder();

                    using (var progress = new ASCIIProgressBar())
                    {
                        int cnt = 0;

                        for (int i = 0; i < numberOfWindDirs; i++)
                        {
                            UFile.Append(simulatedWindDirList[i] + " , , ,");

                        }
                        UFile.AppendLine("");
                        for (int i = 0; i < numberOfWindDirs; i++)
                        {
                            UFile.Append("x, y, z,");
                        }
                        UFile.AppendLine("");

                        for (int r = 0; r < numberOfProbes; r++)
                        {
                            for (int c = 0; c < numberOfWindDirs; c++)
                            {

                                UFile.Append(String.Format("{0:0.##}", AnnualData[c, r].X) + "," + String.Format("{0:0.##}", AnnualData[c, r].Y) + "," + String.Format("{0:0.##}", AnnualData[c, r].Z) + ",");
                                progress.Report((double)cnt / numberOfProbes * numberOfWindDirs);
                                cnt++;
                            }
                            UFile.AppendLine("");
                        }

                    }

                    File.WriteAllText(WorkingDir + @"\U.csv", UFile.ToString());

                    //Write Reduction Array to file


                    Console.WriteLine(Utilities.ConvertComputeTimes(sw4.ElapsedMilliseconds));


                    Console.WriteLine("Write Reduction Array");
                    Stopwatch sw5 = new Stopwatch(); sw5.Start();


                    // Calculate the undisturbed velocity at probing height !!!This only makes sense for horizontal slices!!!

                    using (var progress = new ASCIIProgressBar())
                    {
                        int cnt = 0;



                        var probingHeight = pointList[0].Z;
                        var UProbingHeight = ((0.41 * URef) / Math.Log((zref + z0) / z0) / 0.41) * Math.Log((probingHeight + z0) / z0);


                        System.Text.StringBuilder ReductionFile = new System.Text.StringBuilder();

                        for (int i = 0; i < numberOfWindDirs; i++)
                        {
                            ReductionFile.Append(simulatedWindDirList[i] + ",");
                        }

                        ReductionFile.AppendLine("");
                        for (int r = 0; r < numberOfProbes; r++)
                        {
                            for (int c = 0; c < numberOfWindDirs; c++)
                            {
                                ReductionFile.Append(String.Format("{0:0.#}", Math.Round(Math.Sqrt(Math.Pow(AnnualData[c, r].X, 2) + Math.Pow(AnnualData[c, r].Y, 2) + Math.Pow(AnnualData[c, r].Z, 2)) / UProbingHeight, 3)) + ",");
                                progress.Report((double)cnt / numberOfProbes * numberOfWindDirs);
                                cnt++;
                            }
                            ReductionFile.AppendLine("");
                        }
                        File.WriteAllText(WorkingDir + @"\

", ReductionFile.ToString());


                        if (Verbose)
                        {
                            File.WriteAllText(WorkingDir + @"\Probes.err", errorLog.ToString());
                        }

                        Console.WriteLine(Utilities.ConvertComputeTimes(sw5.ElapsedMilliseconds));

                        Console.WriteLine("Done");


                    }

                }
            }


            catch (Exception e) { Console.WriteLine(e.Message); File.WriteAllText(WorkingDir + @"\Probes.err", errorLog.ToString()); return; }
        }


        private static double GetWindReductionFactor(int probeIndex, double[][] ReductionArray, int numberOfProbes, List<int> windDirsSimulated, double windVelWeatherFile, double windDirFromWeatherFile)
        {

            int numberOfWindDirs = windDirsSimulated.Count();

            // 0, 45, 90, 135, 180, 225, 270, 315, 360


            var nextLowIndex = ReturnNextLowerIndex(windDirsSimulated, windDirFromWeatherFile);
            var nextUpIndex = ReturnNextUpperIndex(windDirsSimulated, windDirFromWeatherFile);

            var nextLowDir = windDirsSimulated[nextLowIndex];
            var nextUpDir = windDirsSimulated[nextUpIndex];


            double distanceToLower = windDirFromWeatherFile - windDirsSimulated[nextLowIndex];
            double distanceToUpper;

            if (nextUpIndex == 0)
            {
                distanceToUpper = Math.Abs((windDirFromWeatherFile - windDirsSimulated[nextUpIndex]) - 360);
            }
            else
            {
                distanceToUpper = windDirFromWeatherFile - windDirsSimulated[nextUpIndex];
            }


           



            //var y1_y0 = distanceToUpper;
            //var x0 = ReductionArray[nextUpIndex][probeIndex];
            //var x1_x0 = ReductionArray[nextLowIndex][probeIndex] - ReductionArray[nextUpIndex][probeIndex];
            //var y_y0 = distanceToLower + distanceToUpper;
            var weightingLow = 1 - (distanceToLower / (distanceToLower + distanceToUpper));
            var weightingUp = 1 - (distanceToUpper / (distanceToLower + distanceToUpper));
            var nextLowerReduction = ReductionArray[nextLowIndex][probeIndex];
            var nextUpperReduction = ReductionArray[nextUpIndex][probeIndex];

            var windRedFactorInterpolated = ((nextLowerReduction * weightingLow) + (nextUpperReduction * weightingUp));


            //  (ReductionArray[nextLow][probeIndex] + distanceToLower * (ReductionArray[nextUp][probeIndex] / (distanceToLower + distanceToUpper)));


            return windRedFactorInterpolated;
        }


        private static int ReturnNextLowerIndex(List<int> windDirs, double UTCIWindDir)
        {


            int lowerIndex = 0;
            int NextLower = windDirs[0];

            for (int i = 0; i < windDirs.Count(); i++)
            {
                if (windDirs[i] < UTCIWindDir)
                {
                    NextLower = windDirs[i];
                    lowerIndex = i;
                }
            }

            return lowerIndex;
        }
        private static int ReturnNextUpperIndex(List<int> windDirs, double UTCIWindDir)
        {
            // Make sure that 360 input is equal to 0
            if (UTCIWindDir == 360)
            {
                UTCIWindDir = 0;
            }


            int upperIndex = windDirs.Count - 1;
            int NextUpper = windDirs[0];

            //Add 360 to enable comparison with "0" degrees
            for (int i = 0; i < windDirs.Count; i++)
            {
                if (windDirs[i] == 0)
                {
                    windDirs.Add(360);
                }
            }

            for (int i = windDirs.Count - 1; i > 0; i--)
            {
                if (windDirs[i] > UTCIWindDir)
                {
                    NextUpper = windDirs[i];
                    upperIndex = i;
                }
            }

            return upperIndex;
        }




    }
}
