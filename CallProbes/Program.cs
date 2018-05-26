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


                var dirs = options.dirs.Split(',');


                //[prope][x,y,z]
                double[][] probes = EddyLib.RadianceFiles.readPTS(options.probes);

                List<Point3d> pointList = new List<Point3d>();

                for (int i = 0; i < probes.GetLength(0); i++)
                {
                    pointList.Add(new Point3d(probes[i][0], probes[i][1], probes[i][2]));
                }



                var cpTree = new List<List<double>>();
                var uTree = new List<List<double[]>>();


                if (options.mode == 0) // cp
                {


                    StringBuilder command = new StringBuilder();

                    string pointName = "cp_Probes";
                    string OFfield = "total(p)_coeff";

                    for (int i = 0; i < dirs.Length; i++)
                    {

                        //File.WriteAllText(options.workingDir + dirs[i] + @"\system\" + "controlDict", StringTemplates.controlDict(DOM, null, i));
                        //File.WriteAllText(options.workingDir + dirs[i] + @"\system\" + pointName, StringTemplates.sampleProbes(listOfPoints, pointName, options.mode));
                        command.Append(@"postProcess -case " + dirs[i] + " -func " + pointName + @" -latestTime;");


                    }

                    ProcessStartInfo psi = new ProcessStartInfo(EddyLib.Utilities.AssemblyDirectory + @"\CallOF.exe", @" -e """ + command + @""" -f " + "\"" + options.workingDir);
                    Process p = new Process();
                    p.StartInfo = psi;
                    p.Start();
                    p.WaitForExit();

                    for (int i = 0; i < dirs.Length; i++)
                    {
                        ParsingValues cp = new ParsingValues(pointList, pointName, options.workingDir + "\\" + dirs[i], OFfield);
                        //cpTree.AddRange(cp.cpValues, new Grasshopper.Kernel.Data.GH_Path(i));
                    }


                }

                if (options.mode == 1) // U
                {


                    StringBuilder command = new StringBuilder();

                    string pointName = "U_Probes";
                    string OFfield = "U";

                    for (int i = 0; i < dirs.Length; i++)
                    {


                        // Write the dicts

                        //File.WriteAllText(options.workingDir + dirs[i] + @"\system\" + pointName, StringTemplates.sampleProbes(listOfPoints, pointName, options.mode));
                        command.Append(@"postProcess -case " + dirs[i] + " -func " + pointName + @" -latestTime;");


                    }

                    ProcessStartInfo psi = new ProcessStartInfo(Utilities.AssemblyDirectory + @"\CallOF.exe", @" -e """ + command + @""" -f " + "\"" + options.workingDir);
                    Process p = new Process();
                    p.StartInfo = psi;
                    p.Start();
                    p.WaitForExit();

                    for (int i = 0; i < dirs.Length; i++)
                    {

                        // Parse values

                        var U = new ParsingValues(pointList, pointName, options.workingDir + "\\" + dirs[i], OFfield);


                        // Create datatree

                        // uTree.AddRange(U.uValues, new Grasshopper.Kernel.Data.GH_Path(i));

                    }



                }
            }







        }  
    }


    // Define a class to receive parsed values
    class Options
    {
        [Option('w', "workingDir", Required = true,
        HelpText = "Working directory.")]
        public string workingDir { get; set; }

        [Option('p', "probes", Required = true, 
        HelpText = "Probes file (.pts)")]
        public string probes { get; set; }

    [Option('d', "dirs", Required = true,
        HelpText = "Wind directions as comma separated string - > 0,45,90")]
    public string dirs { get; set; }

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
