using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;

namespace EddyLib
{
    public class AnnualVelocities
    {
        //[probes, windDirs]  Vector3d[,] Probes;

        public Vector3d[,] Values;
        public int[] WindDirs;

        public bool resultPrecalculated;
        public bool wrongNumberOfProbes;
        public bool infValues;

        public AnnualVelocities(int[] windDirs, Vector3d[,] vectors, string csvFilePath, bool truncateDoubles, bool recalc, int truncateBy = 1)
        {
            this.infValues = CheckForInfValues(vectors);

            if (File.Exists(csvFilePath) && !recalc)
            {
                var temp = ReadAnnualVelocitiesFromCSV(csvFilePath);

                if (temp.Item2.GetLength(0) == vectors.GetLength(0))
                {
                    this.Values = temp.Item2;
                    this.WindDirs = temp.Item1;
                    this.resultPrecalculated = true;
                    this.wrongNumberOfProbes = false;
                }
                else
                {
                    this.wrongNumberOfProbes = true;
                    this.resultPrecalculated = false;
                }
            }
            if (recalc)
            {
                if (File.Exists(csvFilePath))
                {
                    File.Delete(csvFilePath);
                }

                WriteAnnualVel2CSV(windDirs, vectors, csvFilePath, truncateDoubles, truncateBy);
                this.Values = ReadAnnualVelocitiesFromCSV(csvFilePath).Item2;
                this.WindDirs = ReadAnnualVelocitiesFromCSV(csvFilePath).Item1;
                this.resultPrecalculated = false;
                this.wrongNumberOfProbes = false;
            }
        }

        //private static void WriteAnnualVelocityProbes(BoundaryConditions bcond, string baseWorkingDir, string csvAnnualVelocityProbes)
        //{
        //    List<string> fullProbeFilePath = new List<String>();

        // //Build paths as list

        // for (int i = 0; i < bcond.windDirs.Count; i++) { fullProbeFilePath.Add(baseWorkingDir +
        // "\\" + bcond.windDirs[i] + @"\postProcessing\U_Probes.csv"); }

        // var numberOfWindDirs = bcond.windDirs.Count(); var numberOfProbes =
        // File.ReadAllLines(fullProbeFilePath[0]).Count(); //string[] abc = replacedString.Split("
        // ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

        // // Array for output data

        // var listOfAnnualData = new Vector3d[numberOfWindDirs][];

        // for (int r = 0; r < numberOfWindDirs; r++) { listOfAnnualData[r] = new
        // Vector3d[numberOfProbes]; //int counter = 1; for (int c = 0; c < numberOfProbes; c++) {
        // listOfAnnualData[r][c] = new
        // Vector3d(double.Parse(File.ReadAllLines(fullProbeFilePath[r])[c].Split(",".ToCharArray(),
        // StringSplitOptions.RemoveEmptyEntries)[0]) / bcond.UPedestrianHeight,
        // double.Parse(File.ReadAllLines(fullProbeFilePath[r])[c].Split(",".ToCharArray(),
        // StringSplitOptions.RemoveEmptyEntries)[1]) / bcond.UPedestrianHeight,
        // double.Parse(File.ReadAllLines(fullProbeFilePath[r])[c].Split(",".ToCharArray(),
        // StringSplitOptions.RemoveEmptyEntries)[2]) / bcond.UPedestrianHeight); //counter += 3; } }

        // // Write Array to dataTree //for (int c = 0; c < numberOfWindDirs; c++) //{ // for (int r
        // = 0; r < numberOfProbes; r++) // { // UTree.Add(listOfAnnualData[c][r], new
        // Grasshopper.Kernel.Data.GH_Path(c)); // }

        // //}

        // //DA.SetDataTree(0, UTree);

        // //Write U Array to file System.Text.StringBuilder UFile = new System.Text.StringBuilder();

        // for (int i = 0; i < bcond.windDirs.Count; i++) { UFile.AppendLine(bcond.windDirs[i] + ", ,
        // ,"); UFile.AppendLine("x, y, z,"); }

        // UFile.AppendLine("");

        // for (int r = 0; r < numberOfProbes; r++) { for (int c = 0; c < numberOfWindDirs; c++) {
        // UFile.AppendLine(listOfAnnualData[c][r] + ","); } UFile.AppendLine(""); }
        // File.WriteAllText(baseWorkingDir + @"\hourlyU.csv", UFile.ToString());

        // //Write Reduction Array to file

        // System.Text.StringBuilder ReductionFile = new System.Text.StringBuilder();

        // for (int i = 0; i < bcond.windDirs.Count; i++) { ReductionFile.Append(bcond.windDirs[i] +
        // ","); }

        //    ReductionFile.AppendLine("");
        //    for (int r = 0; r < numberOfProbes; r++)
        //    {
        //        for (int c = 0; c < numberOfWindDirs; c++)
        //        {
        //            //ReductionFile.Append(Math.Sqrt(Math.Pow(listOfAnnualData[c][r].X,2)* Math.Pow(listOfAnnualData[c][r].Y,2)* Math.Pow(listOfAnnualData[c][r].Z,2) )+ ",");
        //            ReductionFile.Append(Math.Round(listOfAnnualData[c][r].Length, 3) + ",");
        //        }
        //        ReductionFile.AppendLine("");
        //    }
        //    File.WriteAllText(baseWorkingDir + csvAnnualVelocityProbes, ReductionFile.ToString());
        //}

        //private Vector3d[,] AggregateVectors(List<Vector3d> vectors)
        //{
        //    foreach (List)

        //        return arrr;
        //}

        private bool CheckForInfValues(Vector3d[,] vectors)
        {
            bool infValues = false;
            foreach (Vector3d vec in vectors)
            {
                if (vec.Length > 10000) { infValues = true; }
            }
            return infValues;
        }

        public static Tuple<int[], Vector3d[,]> ReadAnnualVelocitiesFromCSV(string filePath)
        {
            // Todo: slow, fix later

            // data.GetUpperBound(1) wind dirs data.GetUpperBound(0) probes

            var data = RadianceFiles.readCSVFile(filePath);

            // Extract Header

            var WindDirs = new List<int>();

            for (int d = 0; d < data.GetUpperBound(1); d += 3)
            {
                WindDirs.Add((int)data[0, d]);
            }

            // data.GetUpperBound(1)+1/3 because last line always has an additional comma and we have
            // 3 vector components

            var Probes = new Vector3d[data.GetUpperBound(0), (data.GetUpperBound(1) + 1) / 3];

            // int p = 1 --> Skip header

            Parallel.For(1, data.GetUpperBound(0) + 1, p =>
            {
                // int d = 0;
                int cnt = 0;
                for (int d = 0; d < (data.GetUpperBound(1) + 1) / 3; d++)
                {
                    // p-1 because we want to start with 0
                    Probes[p - 1, d] = new Vector3d(data[p, cnt], data[p, cnt + 1], data[p, cnt + 2]);
                    cnt += 3;
                }
            });

            // Vector3d[,] res = new Vector3d[,]{{ new Vector3d(1, 1, 1)}};

            return new Tuple<int[], Vector3d[,]>(WindDirs.ToArray(), Probes);
        }

        public static void WriteAnnualVel2CSV(int[] windDirs, Vector3d[,] vectors, string filePath, bool truncateDoubles, int truncateBy = 1)
        {
            //writing output to csv

            StringBuilder sb = new StringBuilder();

            string header = "";

            for (int d = 0; d <= vectors.GetUpperBound(1); d++)
            {
                header += windDirs[d] + ", , ,";
            }
            sb.AppendLine(header);

            if (!truncateDoubles)
            {
                truncateBy = 3;
            }
            else
            {
                truncateBy = 1;
            }

            for (int p = 0; p <= vectors.GetUpperBound(0); p++)
            {
                string content = "";
                for (int d = 0; d <= vectors.GetUpperBound(1); d++)
                {
                    // Filter extreme values
                    if (vectors[p, d].Length > 10000)
                    {
                        content += "0 , 0 , 0 , ";
                        continue;
                    }
                    else
                    {
                        var X = Math.Round(vectors[p, d].X, truncateBy).ToString();
                        var Y = Math.Round(vectors[p, d].Y, truncateBy).ToString();
                        var Z = Math.Round(vectors[p, d].Z, truncateBy).ToString();
                        content += X + "," + Y + "," + Z + ",";
                    }
                }
                sb.AppendLine(content);
            }
            File.WriteAllText(filePath, sb.ToString());
        }
    }

    public class WindFactors
    {
        public int[] clstSimDirs;
        public int[] Indices;
        public int[] offSet;
        public double offSetAverage;
        public double[,] ValuesWindFactors;
        public double[] ValuesPedestrianComfort;

        public bool resultPrecalculated;
        public bool wrongNumberOfProbes;

        public WindFactors(string baseWorkingDir, BoundaryConditions bcond, Weather weather, AnnualVelocities velocityProbes, double probingHeight, bool interpolate, bool recalc)
        {
            var csvWindFactors = baseWorkingDir + @"WindFactors.csv";

            if (File.Exists(csvWindFactors) && !recalc)
            {
                try
                {
                    this.ValuesWindFactors = ReadWindReductionArrayFromCSV(csvWindFactors);
                    this.resultPrecalculated = true;
                    this.wrongNumberOfProbes = false;

                    var (SimDirIndices, ClstSimDirs, OffSet, OffSetAverage) = GetClosestWindDirs(weather, bcond);
                    this.offSet = OffSet.ToArray();
                    this.offSetAverage = OffSet.Average();
                    this.clstSimDirs = ClstSimDirs.ToArray();
                    this.Indices = SimDirIndices.ToArray();
                }
                catch (Exception e)
                {
                    this.resultPrecalculated = false;
                    this.wrongNumberOfProbes = true;
                    throw e;
                }
            }
            else
            {
                var (SimDirIndices, ClstSimDirs, OffSet, OffSetAverage) = GetClosestWindDirs(weather, bcond);
                this.offSet = OffSet.ToArray();
                this.offSetAverage = OffSet.Average();
                this.clstSimDirs = ClstSimDirs.ToArray();
                this.Indices = SimDirIndices.ToArray();

                this.ValuesWindFactors = CalcWindReductionArray(velocityProbes.Values, Indices, bcond, weather, probingHeight, interpolate);
                ArrayHelper._2DArray2CSV(this.ValuesWindFactors, csvWindFactors, true, 1);
                this.resultPrecalculated = false;
                this.wrongNumberOfProbes = false;
            }

            this.ValuesPedestrianComfort = CalcPedestrianComfort(this.ValuesWindFactors);
        }

        private double[] CalcPedestrianComfort(double[,] ValuesWindFactors)
        {
            int sensorPointCount = ValuesWindFactors.GetLength(1);
            var pedestrianComfort = new double[sensorPointCount];

            for (int probe = 0; probe < sensorPointCount; probe++)
            {
                var column = ArrayHelper.CustomArray<double>.GetColumn(ValuesWindFactors, probe);

                pedestrianComfort[probe] = CalcPedestrianComfort(column);
            }

            return pedestrianComfort;
        }

        public int CalcPedestrianComfort(double[] annualVelocity)

        {
            int pedestrianComfort = 0;

            // https://www.cibse.org/getmedia/af08491f-ef5b-4f7e-9d70-2b0e36d748ae/01-Wind-Analogue-or-digital.pdf

            // 4:  > 4 m / s "Sitting" Light breezes desired for outdoor restaurants and seating
            // areas where one can read a paper of comfortably sit for long periods
            // 6:  > 6 m / s "Standing" Gentle breezes suitable for main buildings entrances, pick -
            // up / drop off points and bus stops
            // 8:  > 8 m / s “Leisure Walking or Strolling“ Moderate breezes that would be
            // appropriate for walking down a city centre street, park or plaza
            // 10:  > 10 m / s "Business Walking“ Relatively high speeds that can be tolerated if
            // ones objective is to walk, run or cycle without lingering
            // 12:  > 12 m / s “Uncomfortable“ Winds of this magnitude are considered a nuisance for
            // most activities, and wind mitigation is typically recommended

            int n = 12;
            while (n > 3)
            {
                var count = annualVelocity.Where(num => num >= n).Count();
                if (count >= 438) { pedestrianComfort = n; break; }
                n += -2;
            }
            return pedestrianComfort;
        }

        private static double[,] VectorLengths(int sensorPointCount, int numberOfWindDirs, Vector3d[,] vectorProbes)
        {
            var velocities = new double[sensorPointCount, numberOfWindDirs];

            Parallel.For(0, numberOfWindDirs, d =>
            {
                for (int p = 0; p < sensorPointCount; p++)
                {
                    velocities[p, d] = vectorProbes[p, d].Length;
                }
            });

            return velocities;
        }

        public static double GetVelocityAtProbingHeightFromABL(double URefEPW, BoundaryConditions bcond, double probingHeight)
        {
            var zref = bcond.zref;
            var z0 = bcond.z0;

            var UAtProbingHeightFromEPW = ((0.41 * URefEPW) / Math.Log((zref + z0) / z0) / 0.41) * Math.Log((probingHeight + z0) / z0);
            return UAtProbingHeightFromEPW;
        }

        // This returns the plain annual array

        public static double[,] ReadWindReductionArrayFromCSV(string filePath)
        {
            return RadianceFiles.readCSVFile(filePath);
        }

        public Tuple<List<int>, List<int>, List<int>, double> GetClosestWindDirs(Weather weather, BoundaryConditions bcond)
        {
            var offSet = new List<int>();
            var Indices = new List<int>();
            var clstSimDirs = new List<int>();

            var windDirsEPW = weather.WindDirection;
            var windDirSim = bcond.windDirs;

            for (int h = 0; h < 8760; h++)
            {
                int weatherDir = (int)weather.WindDirection[h];
                int closestIndex = 0;
                var distance = 0;

                // Treat 360 as 0 and add that right away if it exists
                if (weatherDir == 360 && bcond.windDirs.Contains(0))
                {
                    closestIndex = 0;

                    distance = 0;
                    offSet.Add(distance);
                    Indices.Add(closestIndex);
                    clstSimDirs.Add(bcond.windDirs[closestIndex]);

                    continue;
                }

                // Check what is closest for all other cases

                if (bcond.windDirs.Contains(weatherDir))
                {
                    closestIndex = windDirSim.IndexOf(weather.WindDirection[h]);
                }
                else
                {
                    var nextIndexDown = ReturnNextLowerIndexN(windDirSim, (int)windDirsEPW[h]);
                    var nextIndexUp = ReturnNextUpperIndexN(windDirSim, (int)windDirsEPW[h]);

                    var nextDirDown = windDirSim[nextIndexDown];
                    var nextDirUp = windDirSim[nextIndexUp];

                    double distanceToLower = Math.Abs(windDirsEPW[h] - nextDirDown);
                    double distanceToUpper = Math.Abs(windDirsEPW[h] - nextDirUp);

                    closestIndex = distanceToLower < distanceToUpper ? nextIndexDown : nextIndexUp;
                }

                distance = Math.Abs(bcond.windDirs[closestIndex] - weatherDir);
                offSet.Add(distance);
                Indices.Add(closestIndex);
                clstSimDirs.Add(bcond.windDirs[closestIndex]);
            }

            return new Tuple<List<int>, List<int>, List<int>, double>(Indices, clstSimDirs, offSet, offSet.Average());
        }

        public static int ReturnNextLowerIndexN(List<int> list, int compareTo)
        {
            int lowerIndex;

            if (compareTo <= list.Min())
            {
                lowerIndex = list.IndexOf(list.Max());
            }
            else
            {
                // Take everything smaller than compare
                var smaller = list.Where(x => x < compareTo);

                // Take the max from that selection and then take the index
                lowerIndex = list.IndexOf(smaller.Max(y => y));
            }

            return lowerIndex;
        }

        public static int ReturnNextUpperIndexN(List<int> list, int compareTo)
        {
            // If values to compare if larger than everything in the list, return the first in the
            // list which is usually 0

            int upperIndex;

            if (compareTo >= list.Max())
            {
                upperIndex = list.IndexOf(list.Min());
            }
            else
            {
                // Take everything larger than compare
                var larger = list.Where(x => x > compareTo);

                // Take the min from that selection and then take the index
                upperIndex = list.IndexOf(larger.Min(y => y));
            }
            return upperIndex;
        }

        // public static void WriteWindReductionArrayToCSV(string WindDirs, string WorkingDir,
        // BoundaryConditions bcond, string probesFilePath, bool Verbose, out StringBuilder errorLog)
        // { // mode is for cp errorLog = new StringBuilder(); errorLog.AppendLine("test"); var
        // simulatedWindDirList = WindDirs.Split(','); int numberOfWindDirs = simulatedWindDirList.Length;

        // // Read all variables from one file path. Variables are usually identical for all wind
        // directions so this should be robust. var ABLfilePath = WorkingDir + "\\" +
        // WindDirs.Split(',')[0] + @"\0.org\ABLConditions";

        // try { // Error checking if (!Directory.Exists(WorkingDir)) {
        // errorLog.AppendLine(WorkingDir + " not found. Exiting"); Console.WriteLine(WorkingDir + "
        // not found. Exiting"); }

        // if (Utilities.Directories.IsDirectoryEmpty(WorkingDir + @"\mesh\constant\polyMesh")) {
        // errorLog.AppendLine("The mesh folder is empty. Can't pull probes from a mesh that does not
        // exist."); //throw new System.ArgumentException("The mesh folder is empty. Can't pull
        // probes from a mesh that does not exist."); }

        // for (int i = 0; i < numberOfWindDirs; i++) { var fp = WorkingDir + @"\" +
        // simulatedWindDirList[i] + @"\system\U_Probes"; if (!File.Exists(fp)) {
        // errorLog.AppendLine(@"The wind direction """ + simulatedWindDirList[i] + @""" misses the
        // probing dictionary. Please connect the ""writeProbes"" component and recompute the
        // solution."); throw new System.ArgumentException("The wind direction " +
        // simulatedWindDirList[i] + @" misses the probing dictionary. Please connect the component
        // ""writeProbes"" and recompute the solution."); } }

        // for (int i = 0; i < numberOfWindDirs; i++) { var fp = WorkingDir + @"\" +
        // simulatedWindDirList[i] + @"\constant\polyMesh"; if (!Directory.Exists(fp)) {
        // errorLog.AppendLine(@"The wind direction """ + simulatedWindDirList[i] + @""" misses the
        // ""\constant\polyMesh"" dictionary. Please make sure that directory exists."); throw new
        // System.ArgumentException("The wind direction " + simulatedWindDirList[i] + @" misses the
        // ""\constant\polyMesh"" dictionary. Please make sure that directory exists."); } }

        // for (int i = 0; i < numberOfWindDirs; i++) { ABLfilePath = WorkingDir + "\\" +
        // simulatedWindDirList[i] + @"\0.org\ABLConditions"; if (!File.Exists(ABLfilePath)) {
        // Console.WriteLine(ABLfilePath + " not found. Exiting"); errorLog.AppendLine(ABLfilePath +
        // " not found. Exiting"); } }

        // // Check if U file is in last iteration for (int i = 0; i < numberOfWindDirs; i++) {
        // string iter = Utilities.GetLastIterationFromDirectory(WorkingDir + @"\" +
        // simulatedWindDirList[i]).ToString(); string fp = WorkingDir + @"\" +
        // simulatedWindDirList[i] + @"\" + iter + @"\U";

        // if (!File.Exists(fp)) { errorLog.AppendLine(@"The simulation folder of the wind direction
        // """ + simulatedWindDirList[i] + @""" misses the velocity (U) result file. Please make sure
        // that U is calculated for this particular timestep (change WriteInterval) and recompute the
        // solution."); throw new System.ArgumentException(@"The simulation folder of the wind
        // direction """ + simulatedWindDirList[i] + @""" misses the velocity (U) result file. Please
        // make sure that U is calculated for this particular timestep (change WriteInterval) and
        // recompute the solution."); } }

        // // Delete files in subfolders var listOfDirsInfo = new List<string>();

        // for (int i = 0; i < numberOfWindDirs; i++) { listOfDirsInfo.Add((@"C:\Temp\" +
        // simulatedWindDirList[i] + @"\postProcessing\")); }

        // //for (int i = 0; i < numberOfWindDirs; i++) //{ // foreach (var subDir in new
        // DirectoryInfo(listOfDirsInfo[i]).GetDirectories()) // { // if (subDir.ToString().ToLower()
        // == "residuals") // { // continue; // } // subDir.Delete(true); // } //} double URef;
        // double z0; double zref;

        // Utilities.ParseABLConditionsFromCaseFolder(ABLfilePath, out URef, out z0, out zref);

        // double[][] probes = EddyLib.RadianceFiles.readPTS(probesFilePath); var numberOfProbes = probes.GetLength(0);

        // List<Point3d> pointList = new List<Point3d>();

        // for (int i = 0; i < probes.GetLength(0); i++) { pointList.Add(new Point3d(probes[i][0],
        // probes[i][1], probes[i][2])); }

        // Console.WriteLine("Probing the simulation results.");

        // Stopwatch sw = new Stopwatch(); sw.Start();

        // StringBuilder command = new StringBuilder();

        // string pointName = "U_Probes"; string OFfield = "U";

        // for (int i = 0; i < numberOfWindDirs; i++) { // Write the dicts
        // //File.WriteAllText(options.workingDir + dirs[i] + @"\system\" + pointName,
        // EddyLib.StringTemplatessampleProbes(listOfPoints, pointName, options.mode));
        // command.Append(@"postProcess -case " + simulatedWindDirList[i] + " -func " + pointName +
        // @" -latestTime | tee -a " + simulatedWindDirList[i] + @"/log_probes;"); }

        // ProcessStartInfo psi = new ProcessStartInfo(Utilities.AssemblyDirectory + @"\CallOF.exe",
        // @" -e """ + command + @""" -f " + "\"" + WorkingDir); Process p = new Process { StartInfo
        // = psi }; p.Start(); p.WaitForExit(); p.Close();

        // // Issue // Could not find a part of the path 'C:\temp\0\PostProcessing\U_Probes'. // This
        // happens if OF process closes immideately after calling

        // //Thread.Sleep(2 * 30* Math.Sqrt(probes.GetLength(0)) * numberOfWindDirs);

        // Console.WriteLine(Utilities.ConvertComputeTimes(sw.ElapsedMilliseconds));

        // Console.WriteLine("Parsing the velocity vectors for the probes of every wind direction and
        // writing result files."); Stopwatch sw2 = new Stopwatch(); sw2.Start();

        // for (int i = 0; i < numberOfWindDirs; i++) { // Parse values //Thread.Sleep(2 *
        // probes.GetLength(0)); var ofField = new OFField(OFfield, pointName); var U = new
        // Probing(pointList, WorkingDir + "\\" + simulatedWindDirList[i], WorkingDir, ofField, int.Parse(simulatedWindDirList[i]));

        // // Create datatree

        // // uTree.AddRange(U.uValues, new Grasshopper.Kernel.Data.GH_Path(i)); }

        // List<string> fullProbeFilePath = new List<String>();

        // //var numberOfProbes = File.ReadAllLines(fullProbeFilePath[0]).Count(); //defined above
        // //string[] abc = replacedString.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

        // //Build list of paths

        // for (int i = 0; i < numberOfWindDirs; i++) { var path = WorkingDir + "\\" +
        // simulatedWindDirList[i] + @"\postProcessing\U_Probes.csv"; if (!File.Exists(path)) {
        // Console.WriteLine(path + " not found. Exiting"); errorLog.AppendLine(path + " not found.
        // Exiting"); return; } fullProbeFilePath.Add(path); }

        // Console.WriteLine(Utilities.ConvertComputeTimes(sw2.ElapsedMilliseconds));

        // // Array for output data

        // Console.WriteLine("Re-collecting output data from every wind direction."); Stopwatch sw3 =
        // new Stopwatch(); sw3.Start();

        // Vector3d[,] AnnualData = new Vector3d[numberOfWindDirs, numberOfProbes];

        // var UData = new string[numberOfWindDirs][];

        // for (int i = 0; i < numberOfWindDirs; i++) { UData[i] =
        // File.ReadAllLines(fullProbeFilePath[i]); }

        // //UData[0] = File.ReadAllLines(fullProbeFilePath[0]); //UData[1] =
        // File.ReadAllLines(fullProbeFilePath[1]); //UData[2] =
        // File.ReadAllLines(fullProbeFilePath[2]); //UData[3] =
        // File.ReadAllLines(fullProbeFilePath[3]); //UData[4] =
        // File.ReadAllLines(fullProbeFilePath[4]); //UData[5] =
        // File.ReadAllLines(fullProbeFilePath[5]); //UData[6] =
        // File.ReadAllLines(fullProbeFilePath[6]); //UData[7] = File.ReadAllLines(fullProbeFilePath[7]);

        // using (var progress = new ASCIIProgressBar()) { int cnt = 0; Parallel.For(0,
        // numberOfWindDirs, r =>

        // { //for (int r = 0; r < numberOfWindDirs; r++) //{ //listOfAnnualData[r] = new
        // Vector3d[numberOfProbes]; for (int c = 0; c < numberOfProbes; c++) { AnnualData[r, c] =
        // new Vector3d(double.Parse(UData[r][c].Split(',')[0]),
        // double.Parse(UData[r][c].Split(',')[1]), double.Parse(UData[r][c].Split(',')[2]));
        // progress.Report((double)cnt / numberOfProbes * numberOfWindDirs); cnt++; } //} }); }

        // Console.WriteLine(Utilities.ConvertComputeTimes(sw3.ElapsedMilliseconds));

        // //Write U Array to file Console.WriteLine("Writing U Array"); Stopwatch sw4 = new
        // Stopwatch(); sw4.Start();

        // System.Text.StringBuilder UFile = new System.Text.StringBuilder();

        // using (var progress = new ASCIIProgressBar()) { int cnt = 0;

        // for (int i = 0; i < numberOfWindDirs; i++) { UFile.Append(simulatedWindDirList[i] + " , ,
        // ,"); } UFile.AppendLine(""); for (int i = 0; i < numberOfWindDirs; i++) { UFile.Append("x,
        // y, z,"); } UFile.AppendLine("");

        // for (int r = 0; r < numberOfProbes; r++) { for (int c = 0; c < numberOfWindDirs; c++) {
        // UFile.Append(String.Format("{0:0.##}", AnnualData[c, r].X) + "," +
        // String.Format("{0:0.##}", AnnualData[c, r].Y) + "," + String.Format("{0:0.##}",
        // AnnualData[c, r].Z) + ","); progress.Report((double)cnt / numberOfProbes *
        // numberOfWindDirs); cnt++; } UFile.AppendLine(""); } }

        // File.WriteAllText(WorkingDir + @"\U.csv", UFile.ToString());

        // //Write Reduction Array to file

        // Console.WriteLine(Utilities.ConvertComputeTimes(sw4.ElapsedMilliseconds));

        // Console.WriteLine("Write Reduction Array"); Stopwatch sw5 = new Stopwatch(); sw5.Start();

        // // Calculate the undisturbed velocity at probing height !!!This only makes sense for
        // horizontal slices!!!

        // using (var progress = new ASCIIProgressBar()) { int cnt = 0;

        // var probingHeight = pointList[0].Z; var UProbingHeight = ((0.41 * URef) / Math.Log((zref +
        // z0) / z0) / 0.41) * Math.Log((probingHeight + z0) / z0);

        // System.Text.StringBuilder ReductionFile = new System.Text.StringBuilder();

        // for (int i = 0; i < numberOfWindDirs; i++) { ReductionFile.Append(simulatedWindDirList[i]
        // + ","); }

        // ReductionFile.AppendLine(""); for (int r = 0; r < numberOfProbes; r++) { for (int c = 0; c
        // < numberOfWindDirs; c++) { ReductionFile.Append(String.Format("{0:0.#}",
        // Math.Round(Math.Sqrt(Math.Pow(AnnualData[c, r].X, 2) + Math.Pow(AnnualData[c, r].Y, 2) +
        // Math.Pow(AnnualData[c, r].Z, 2)) / UProbingHeight, 3)) + ","); progress.Report((double)cnt
        // / numberOfProbes * numberOfWindDirs); cnt++; } ReductionFile.AppendLine(""); }
        // File.WriteAllText(WorkingDir + @"\

        //", ReductionFile.ToString());

        // if (Verbose) { File.WriteAllText(WorkingDir + @"\Probes.err", errorLog.ToString()); }

        // Console.WriteLine(Utilities.ConvertComputeTimes(sw5.ElapsedMilliseconds));

        // Console.WriteLine("Done"); }

        // } catch (Exception e) { Console.WriteLine(e.Message); File.WriteAllText(WorkingDir +
        // @"\Probes.err", errorLog.ToString()); return; } }

        //(List<int> SimDirIndices, List<int> ClstSimDirs, List<int> OffSet, double OffSetAverage) GetClosestWindDirs(Weather weather, BoundaryConditions bcond)
        //{
        //    /////////// closest indices
        //    ///
        //    var offSet = new List<int>();
        //    var Indices = new List<int>();
        //    var clstSimDirs = new List<int>();

        // foreach (int hour in weather.WindDirection) { int weatherDir =
        // (int)weather.WindDirection[hour]; int closestIndex = 0;

        // if (bcond.windDirs.Contains(weatherDir)) { closestIndex =
        // bcond.windDirs.IndexOf(weather.WindDirection[hour]); } else { int distanceToUpper = 0;

        // var nextUpper = ReturnNextUpperIndex(bcond.windDirs, weatherDir, out distanceToUpper);

        // //distanceToUpper = distanceToUpper;

        // int distanceToLower = 0;

        // var nextLower = ReturnNextLowerIndex(bcond.windDirs, weatherDir, out distanceToLower);

        // //distanceToLower = distanceToLower;

        // // Pick smaller of the two closestIndex = distanceToLower < distanceToUpper ? nextLower : nextUpper;

        // //_distanceToUpper = distanceToUpper; //_distanceToLower = distanceToLower;

        // }

        // var distance = Math.Abs(bcond.windDirs[closestIndex] - weatherDir); offSet.Add(distance);
        // Indices.Add(closestIndex); clstSimDirs.Add(bcond.windDirs[closestIndex]); }

        // return (Indices, clstSimDirs, offSet, offSet.Average()); // tuple literal

        //}

        private static double[,] CalcWindReductionArray(Vector3d[,] annualVecProbes, int[] clstSimDirIdx, BoundaryConditions bcond, Weather weather, double probingHeight, bool interpolate)
        {
            //[probes, windDirs]  Vector3d[,] annualVecProbes;

            var windDirSim = bcond.windDirs;
            int numberOfWindDirs = windDirSim.Count();
            int sensorPointCount = annualVecProbes.GetLength(0);
            int numberOfHours = 8760;
            int rounding = 1;

            double[,] WF = new double[numberOfHours, sensorPointCount];

            int cntReduction = 0;
            using (var progress = new ASCIIProgressBar())
            {
                // Calculate vector lengths

                var annualVelocities = VectorLengths(sensorPointCount, numberOfWindDirs, annualVecProbes);

                Console.WriteLine("Calculating: Wind reduction factors");

                var windDirsEPW = weather.WindDirection;

                // Calculate indeces first

                int[] nextIndexDown = new int[8760];
                int[] nextIndexUp = new int[8760];

                Parallel.For(0, numberOfHours, h =>
                {
                    nextIndexDown[h] = ReturnNextLowerIndexN(windDirSim, (int)windDirsEPW[h]);
                    nextIndexUp[h] = ReturnNextUpperIndexN(windDirSim, (int)windDirsEPW[h]);
                });

                Parallel.For(0, numberOfHours, h =>
        {
            for (int p = 0; p < sensorPointCount; p++)
            {
                var nextDirDown = windDirSim[nextIndexDown[h]];
                var nextDirUp = windDirSim[nextIndexUp[h]];

                double distanceToLower = Math.Abs(windDirsEPW[h] - nextDirDown);
                double distanceToUpper = Math.Abs(windDirsEPW[h] - nextDirUp);

                var velAtProbHeightEPW = GetVelocityAtProbingHeightFromABL(weather.WindSpeed[h], bcond, probingHeight);
                var velSim = annualVelocities[p, clstSimDirIdx[h]];
                var velApproaching = GetVelocityAtProbingHeightFromABL(bcond.URef, bcond, probingHeight);
                // Avoid Infinity
                var ratio = velApproaching == 0 ? 0.00000 : velSim / velApproaching;

                // We need to multiply the normalized velocity with respect to the approaching flow
                // for every probiing point and multiply that with the scaled-down, measured airport velocity.

                if (!interpolate)
                {
                    WF[h, p] = Math.Round(velAtProbHeightEPW * ratio, rounding);
                }
                else
                {
                    var weightingDown = 1 - (distanceToLower / (distanceToLower + distanceToUpper));
                    var weightingUp = 1 - (distanceToUpper / (distanceToLower + distanceToUpper));
                    var nextVelocityDown = annualVelocities[p, nextIndexDown[h]];
                    var nextVelocityUp = annualVelocities[p, nextIndexUp[h]];

                    double weightedVelSim = (nextVelocityDown * weightingDown) + (nextVelocityUp * weightingUp);

                    // Avoid Infinity
                    var ratioWeighted = velApproaching == 0 ? 0.00000 : weightedVelSim / velApproaching;

                    WF[h, p] = Math.Round(velAtProbHeightEPW * ratioWeighted * weightedVelSim, rounding);
                }

                cntReduction++;
                progress.Report((double)cntReduction / sensorPointCount);
            }
        });
            }
            // WF[h, p]
            return WF;
        }
    }
}