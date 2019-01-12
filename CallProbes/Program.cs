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

namespace CallProbes
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // Check licence

            if (Utilities.CheckLicence() == true)
            {
                var options = new Options();
                if (CommandLine.Parser.Default.ParseArguments(args, options))
                {
                    StringBuilder errorLog = new StringBuilder();

                    Console.WriteLine("Working directory: {0}", options.WorkingDir);
                    errorLog.AppendLine(String.Format("Working directory: {0}", options.WorkingDir));

                    Console.WriteLine("Probes: {0}", options.Probes);
                    errorLog.AppendLine(String.Format("Probes: {0}", options.Probes));

                    Console.WriteLine("Wind directions considered: {0}", options.WindDirs);
                    errorLog.AppendLine(String.Format("Wind directions considered: {0}", options.WindDirs));

                    Console.WriteLine("Mode (0=cp;1=U): {0}", options.Mode);
                    errorLog.AppendLine(String.Format("Mode (0=cp;1=U): {0}", options.Mode));

                    Console.WriteLine("Verbose: {0}", options.Verbose);
                    errorLog.AppendLine(String.Format("Verbose: {0}", options.Verbose));



                    double URef = options.uref;
                    double zref = options.zref;
                    double z0 = options.z0;

                    // Read all variables from one file path. Variables are usually identical for all wind directions so this should be robust.
                    var filePath = options.WorkingDir + "\\" + options.WindDirs.Split(',')[0] + @"\0.org\ABLConditions";

                    var windDirs = options.WindDirs.Split(',');
                    var numberOfWindDirs = windDirs.Length;





                    try
                    {

                        // Error checking

                        if (!Directory.Exists(options.WorkingDir)) { errorLog.AppendLine(options.WorkingDir + " not found. Exiting"); Console.WriteLine(options.WorkingDir + " not found. Exiting"); }

                        if (Utilities.IsDirectoryEmpty(options.WorkingDir + @"\mesh\constant\polyMesh"))
                        {
                            errorLog.AppendLine("The mesh folder is empty. Can't pull probes from a mesh that does not exist.");
                            //throw new System.ArgumentException("The mesh folder is empty. Can't pull probes from a mesh that does not exist.");
                        }


                        for (int i = 0; i < numberOfWindDirs; i++)
                        {
                            var fp = options.WorkingDir + @"\" + windDirs[i] + @"\system\U_Probes";
                            if (!File.Exists(fp))
                            {
                                errorLog.AppendLine(@"The wind direction """ + windDirs[i] + @""" misses the probing dictionary. Please connect the ""writeProbes"" component and recompute the solution.");
                                throw new System.ArgumentException("The wind direction " + windDirs[i] + @" misses the probing dictionary. Please connect the component ""writeProbes"" and recompute the solution.");
                            }
                        }

                        for (int i = 0; i < numberOfWindDirs; i++)
                        {
                            var fp = options.WorkingDir + @"\" + windDirs[i] + @"\constant\polyMesh";
                            if (!Directory.Exists(fp))
                            {
                                errorLog.AppendLine(@"The wind direction """ + windDirs[i] + @""" misses the ""\constant\polyMesh"" dictionary. Please make sure that directory exists.");
                                throw new System.ArgumentException("The wind direction " + windDirs[i] + @" misses the ""\constant\polyMesh"" dictionary. Please make sure that directory exists.");
                            }
                        }

                        for (int i = 0; i < numberOfWindDirs; i++)
                        {
                            var ABLfilePath = options.WorkingDir + "\\" + options.WindDirs.Split(',')[i] + @"\0.org\ABLConditions";
                            if (!File.Exists(ABLfilePath)) { Console.WriteLine(ABLfilePath + " not found. Exiting"); errorLog.AppendLine(ABLfilePath + " not found. Exiting"); }
                        }


                        // Check if U file is in last iteration
                        for (int i = 0; i < numberOfWindDirs; i++)
                        {
                            string iter = Utilities.GetLastIterationInSimfolder(options.WorkingDir + @"\" + windDirs[i]).ToString();
                            string fp = options.WorkingDir + @"\" + windDirs[i] + @"\" + iter + @"\U";


                            if (!File.Exists(fp))
                            {
                                errorLog.AppendLine(@"The simulation folder of the wind direction """ + windDirs[i] + @""" misses the velocity (U) result file. Please make sure that U is calculated for this particular timestep (change WriteInterval) and recompute the solution.");
                                throw new System.ArgumentException(@"The simulation folder of the wind direction """ + windDirs[i] + @""" misses the velocity (U) result file. Please make sure that U is calculated for this particular timestep (change WriteInterval) and recompute the solution.");
                            }
                        }





                        // Delete files in subfolders
                        var listOfDirsInfo = new List<string>();

                        for (int i = 0; i < numberOfWindDirs; i++)
                        {
                            listOfDirsInfo.Add((@"C:\Temp\" + windDirs[i] + @"\postProcessing\"));

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



                        Utilities.ParseABLConditionsFromCaseFolder(filePath, out URef, out z0, out zref);




                        double[][] probes = EddyLib.RadianceFiles.readPTS(options.Probes);
                        var numberOfProbes = probes.GetLength(0);

                        List<Point3d> pointList = new List<Point3d>();

                        for (int i = 0; i < probes.GetLength(0); i++)
                        {
                            pointList.Add(new Point3d(probes[i][0], probes[i][1], probes[i][2]));
                        }



                        if (options.Mode == 0) // cp
                        {

                            //try
                            //{


                            //    StringBuilder command = new StringBuilder();

                            //    string pointName = "cp_Probes";
                            //    string OFfield = "total(p)_coeff";

                            //    for (int i = 0; i < numberOfWindDirs; i++)
                            //    {

                            //        //File.WriteAllText(options.workingDir + dirs[i] + @"\system\" + "controlDict", StringTemplates.controlDict(DOM, null, i));
                            //        //File.WriteAllText(options.workingDir + dirs[i] + @"\system\" + pointName, StringTemplates.sampleProbes(listOfPoints, pointName, options.mode));
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

                        if (options.Mode == 1) // U
                        {

                            Console.WriteLine("Probing the simulation results.");

                            Stopwatch sw = new Stopwatch(); sw.Start();



                            StringBuilder command = new StringBuilder();

                            string pointName = "U_Probes";
                            string OFfield = "U";

                            for (int i = 0; i < numberOfWindDirs; i++)
                            {


                                // Write the dicts

                                //File.WriteAllText(options.workingDir + dirs[i] + @"\system\" + pointName, StringTemplates.sampleProbes(listOfPoints, pointName, options.mode));
                                command.Append(@"postProcess -case " + windDirs[i] + " -func " + pointName + @" -latestTime | tee -a  " + windDirs[i] + @"/log_probes;");


                            }



                            ProcessStartInfo psi = new ProcessStartInfo(Utilities.AssemblyDirectory + @"\CallOF.exe", @" -e """ + command + @""" -f " + "\"" + options.WorkingDir);
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
                                int fieldtype = 1; //vectors
                                var U = new ParsingProbes(pointList, pointName, options.WorkingDir + "\\" + windDirs[i], OFfield, fieldtype);

                                // Create datatree

                                // uTree.AddRange(U.uValues, new Grasshopper.Kernel.Data.GH_Path(i));

                            }


                            List<string> fullProbeFilePath = new List<String>();

                            //var numberOfProbes = File.ReadAllLines(fullProbeFilePath[0]).Count(); //defined above                    
                            //string[] abc = replacedString.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);


                            //Build list of paths



                            for (int i = 0; i < numberOfWindDirs; i++)
                            {
                                var path = options.WorkingDir + "\\" + windDirs[i] + @"\postProcessing\U_Probes.csv";
                                if (!File.Exists(path)) { Console.WriteLine(path + " not found. Exiting"); errorLog.AppendLine(path + " not found. Exiting"); return; }
                                fullProbeFilePath.Add(path);
                            }


                            Console.WriteLine(Utilities.ConvertComputeTimes(sw2.ElapsedMilliseconds));




                            // Array for output data

                            Console.WriteLine("Re-collecting output data from every wind direction.");
                            Stopwatch sw3 = new Stopwatch(); sw3.Start();


                            Vector3d[,] AnnualData = new Vector3d[numberOfWindDirs, numberOfProbes];

                            var UData = new string[numberOfWindDirs][];

                            for (int i= 0; i < numberOfWindDirs; i++)
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
                                    UFile.Append(windDirs[i] + " , , ,");

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

                            File.WriteAllText(options.WorkingDir + @"\U.csv", UFile.ToString());

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
                                    ReductionFile.Append(windDirs[i] + ",");
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
                                File.WriteAllText(options.WorkingDir + @"\WindReductionData.csv", ReductionFile.ToString());


                                if (options.Verbose)
                                {
                                    File.WriteAllText(options.WorkingDir + @"\Probes.err", errorLog.ToString());
                                }

                                Console.WriteLine(Utilities.ConvertComputeTimes(sw5.ElapsedMilliseconds));

                                Console.WriteLine("Done");


                            }

                        }
                    }


                    catch (Exception e) { Console.WriteLine(e.Message); File.WriteAllText(options.WorkingDir + @"\Probes.err", errorLog.ToString()); return; }

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
        public double uref { get; set; }

        [Option('r', "z0", Required = true,
        HelpText = "Roughness length")]
        public double z0 { get; set; }

        [Option('z', "zref", Required = true,
        HelpText = "Reference height")]
        public double zref { get; set; }

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
