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
using EddyLib;
using Rhino.Geometry;

namespace CallProbes
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = new Options();
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {

                var lines = File.ReadAllLines(options.workingDir + "\\" + options.windDirs.Split(',')[0] + @"\0.org\ABLConditions");

                double URef = 0.0;
                double zref = 0.0;
                double z0 = 0.0;
                var pedestrianHeight = 1.5;
                
                                                        

                for (int i = 0; i < lines.Length; i++)
                {
                    var l = lines[i];
                    if (l.Contains("Uref")) URef = double.Parse(l.Replace("Uref", "").Replace(";", "").Trim());
                    if (l.Contains("z0")) z0 = double.Parse(l.Replace("z0 uniform", "").Replace(";", "").Trim());
                    if (l.Contains("Zref")) zref = double.Parse(l.Replace("Zref", "").Replace(";", "").Trim());
                }

                var UPedestrianHeight = ((0.41 * URef) / Math.Log((zref + z0) / z0) / 0.41) * Math.Log((pedestrianHeight + z0) / z0);

                var windDirs = options.windDirs.Split(',');
                var numberOfWindDirs = windDirs.Length;


                //[prope][x,y,z]
                double[][] probes = EddyLib.RadianceFiles.readPTS(options.probes);
                var numberOfProbes = probes.GetLength(0);

                List<Point3d> pointList = new List<Point3d>();

                for (int i = 0; i < probes.GetLength(0); i++)
                {
                    pointList.Add(new Point3d(probes[i][0], probes[i][1], probes[i][2]));
                }



                if (Utilities.IsDirectoryEmpty(options.workingDir + @"\mesh\constant\polyMesh") == true)
                {
                    throw new System.ArgumentException("The mesh folder is empty. Can't pull probes from a mesh that does not exist.");
                }


                if (options.mode == 0) // cp
                {


                    

                    StringBuilder command = new StringBuilder();

                    string pointName = "cp_Probes";
                    string OFfield = "total(p)_coeff";

                    for (int i = 0; i < numberOfWindDirs; i++)
                    {

                        //File.WriteAllText(options.workingDir + dirs[i] + @"\system\" + "controlDict", StringTemplates.controlDict(DOM, null, i));
                        //File.WriteAllText(options.workingDir + dirs[i] + @"\system\" + pointName, StringTemplates.sampleProbes(listOfPoints, pointName, options.mode));
                        command.Append(@"postProcess -case " + windDirs[i] + " -func " + pointName + @" -latestTime;");


                    }

                    ProcessStartInfo psi = new ProcessStartInfo(EddyLib.Utilities.AssemblyDirectory + @"\CallOF.exe", @" -e """ + command + @""" -f " + "\"" + options.workingDir);
                    Process p = new Process();
                    p.StartInfo = psi;
                    p.Start();
                    p.WaitForExit();
                    p.Close();
                    

                    for (int i = 0; i < numberOfWindDirs; i++)
                    {
                        //Thread.Sleep(2 * probes.GetLength(0));
                        ParsingValues cp = new ParsingValues(pointList, pointName, options.workingDir + "\\" + windDirs[i], OFfield);
                        //cpTree.AddRange(cp.cpValues, new Grasshopper.Kernel.Data.GH_Path(i));
                    }


                }

                if (options.mode == 1) // U
                {

                    

                    StringBuilder command = new StringBuilder();

                    string pointName = "U_Probes";
                    string OFfield = "U";

                    for (int i = 0; i < numberOfWindDirs; i++)
                    {


                        // Write the dicts

                        //File.WriteAllText(options.workingDir + dirs[i] + @"\system\" + pointName, StringTemplates.sampleProbes(listOfPoints, pointName, options.mode));
                        command.Append(@"postProcess -case " + windDirs[i] + " -func " + pointName + @" -latestTime;");


                    }

                    ProcessStartInfo psi = new ProcessStartInfo(Utilities.AssemblyDirectory + @"\CallOF.exe", @" -e """ + command + @""" -f " + "\"" + options.workingDir);
                    Process p = new Process();
                    p.StartInfo = psi;
                    p.Start();
                    p.WaitForExit();          
                    p.Close();


                    //Thread.Sleep(2 * probes.GetLength(0));


                    for (int i = 0; i < numberOfWindDirs; i++)
                    {

                        // Parse values
                        //Thread.Sleep(2 * probes.GetLength(0));
                        var U = new ParsingValues(pointList, pointName, options.workingDir + "\\" + windDirs[i], OFfield);


                        // Create datatree

                        // uTree.AddRange(U.uValues, new Grasshopper.Kernel.Data.GH_Path(i));

                    }

                    //List<Point3d> points = new List<Point3d>();
                    //DA.GetDataList(1, points);




                    List<string> fullProbeFilePath = new List<String>();
                    
                    //var numberOfProbes = File.ReadAllLines(fullProbeFilePath[0]).Count(); //defined above                    
                    //string[] abc = replacedString.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);


                    //Build paths as list

                    for (int i = 0; i < numberOfWindDirs; i++)
                    {
                        fullProbeFilePath.Add(options.workingDir + "\\" + windDirs[i] + @"\postProcessing\U_Probes.csv");
                    }



                    // Array for output data

                    var listOfAnnualData = new Vector3d[numberOfWindDirs][];

                    for (int r = 0; r < numberOfWindDirs; r++)
                    {
                        listOfAnnualData[r] = new Vector3d[numberOfProbes];
                        //int counter = 1;
                        for (int c = 0; c < numberOfProbes; c++)
                        {
                            listOfAnnualData[r][c] = new Vector3d(double.Parse(File.ReadAllLines(fullProbeFilePath[r])[c].Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0]) / UPedestrianHeight, double.Parse(File.ReadAllLines(fullProbeFilePath[r])[c].Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[1]) / UPedestrianHeight, double.Parse(File.ReadAllLines(fullProbeFilePath[r])[c].Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[2]) / UPedestrianHeight);
                            //counter += 3;
                        }
                    }


                    //Write U Array to file


                    System.Text.StringBuilder UFile = new System.Text.StringBuilder();

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

                            UFile.Append(listOfAnnualData[c][r] + ",");

                        }
                        UFile.AppendLine("");
                    }
                    File.WriteAllText(options.workingDir + @"\hourlyUData.csv", UFile.ToString());

                    //Write Reduction Array to file

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

                            ReductionFile.Append(Math.Round(Math.Sqrt(Math.Pow(listOfAnnualData[c][r].X, 2) * Math.Pow(listOfAnnualData[c][r].Y, 2) * Math.Pow(listOfAnnualData[c][r].Z, 2)), 5) + ",");
                            
                        }
                        ReductionFile.AppendLine("");
                    }
                    File.WriteAllText(options.workingDir + @"\WindReductionData.csv", ReductionFile.ToString());



                }
            }







        }
    }


    // Define a class to receive parsed values
    class Options
    {
        
        [Option('d', "workingDir", Required = true,
        HelpText = "Working directory.")]
        public string workingDir { get; set; }

        [Option('p', "probes", Required = true,
        HelpText = "Probes file (.pts)")]
        public string probes { get; set; }

        [Option('w', "windDirs", Required = true,
            HelpText = "Wind directions as comma separated string - > 0,45,90")]
        public string windDirs { get; set; }

        [Option('m', "mode", Required = true, DefaultValue = 1,
           HelpText = "Mode: 0 = cp, 1 = U")]
        public int mode { get; set; }


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
