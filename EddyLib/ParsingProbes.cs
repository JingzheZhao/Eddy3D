using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.IO;
using Rhino.Geometry;
using System.Collections;

namespace EddyLib
{
    public class ParsingProbes
    {

        public double[] cpValues;
        public Vector3d[] uValues;
        public string valueString;


        private readonly List<Point3d> listOfPoints;
        private readonly string pointName;
        private readonly string caseDirectory;


        public ParsingProbes(List<Point3d> ListOfPoints, string PointName, string caseDirectory, string OFfield)
        {
            listOfPoints = ListOfPoints;
            pointName = PointName;
            this.caseDirectory = caseDirectory;
            if (pointName == "cp_Probes")
            {
                ParsingNumbers(listOfPoints, pointName, this.caseDirectory, OFfield);
            }
            else
            {
                ParsingVectors(listOfPoints, pointName, this.caseDirectory, OFfield);
            }
            WriteToCSV();
        }


        private void WriteToCSV()
        {
            string PostProcessingDirectory = caseDirectory + @"\postProcessing\";
            if (pointName == "cp_Probes")
            {
                StringBuilder sb = new StringBuilder();
                foreach (double i in this.cpValues)
                {
                    sb.AppendLine(i.ToString());
                }
                File.WriteAllText(PostProcessingDirectory + pointName + ".csv", sb.ToString());
            }
            if (pointName == "U_Probes")
            {
                StringBuilder sb = new StringBuilder();
                foreach (Vector3d i in this.uValues)
                {
                    sb.AppendLine(i.ToString());
                }
                File.WriteAllText(PostProcessingDirectory + pointName + ".csv", sb.ToString());
            }


        }


        private void ParsingNumbers(List<Point3d> listOfPoints, string pointName, string workingDirectory, string OFfield)
        {

            string fullPath = GetLastProcProssDir(listOfPoints, pointName, workingDirectory, OFfield);

            var counterPoints = listOfPoints.Count;

            StringBuilder sb = new StringBuilder();


            this.cpValues = new double[counterPoints];
            var lastLine = File.ReadLines(fullPath).Where(line => line != "").Last();

            for (int i = 0; i < counterPoints; i++)
            {
                this.cpValues[i] = double.Parse(lastLine.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[i + 1]); //this workes 
                sb.AppendLine(this.cpValues[i].ToString());
            }
            this.valueString = sb.ToString();


        }

        private void ParsingVectors(List<Point3d> listOfPoints, string pointName, string workingDirectory, string OFfield)
        {

            string fullPath = GetLastProcProssDir(listOfPoints, pointName, workingDirectory, OFfield);

            var counterPoints = listOfPoints.Count;

            StringBuilder sb = new StringBuilder();


            this.uValues = new Vector3d[counterPoints];
            var lastLine = File.ReadLines(fullPath).Last();
            string replacedString = System.Text.RegularExpressions.Regex.Replace(lastLine, "[()]", "", RegexOptions.Compiled);
            string[] abc = replacedString.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            int counter = 1;
            for (int i = 0; i < counterPoints; i++)
            {
                this.uValues[i] = (new Vector3d(double.Parse(abc[counter]), double.Parse(abc[counter + 1]), double.Parse(abc[counter + 2])));
                sb.AppendLine(this.uValues[i].ToString());
                counter += 3;
            }
            this.valueString = sb.ToString();


        }

        public string GetLastProcProssDir(List<Point3d> listOfPoints, string pointName, string workingDirectory, string field)
        {
            var counterPoints = listOfPoints.Count;
            string PostProcessingDirectory = workingDirectory + @"\postProcessing\";


            //replace this with input
            string basePath = PostProcessingDirectory + pointName;

            //string[] filePathResults = new string[counterPoints];
            var directoriesBasePath = Directory.GetDirectories(basePath);
            Array.Sort(directoriesBasePath, new Utilities.NumericComparer());

            var latestTimedirectoriesBasePath = directoriesBasePath[directoriesBasePath.Length - 1];
            string latestTime = Path.GetFileName(latestTimedirectoriesBasePath);

            string fullPath = basePath + @"\" + latestTime + @"\" + field;
            return fullPath;
        }

        public string GetLastIterationPath(string workingDirectory)
        {
            var sortedWorkingDir = Directory.GetDirectories(workingDirectory);
            Array.Sort(sortedWorkingDir, new Utilities.NumericComparer());

            var lastIteration = sortedWorkingDir[sortedWorkingDir.Length - 1];
            string latestTime = Path.GetDirectoryName(lastIteration);

            string fullPath = workingDirectory + @"\" + latestTime;
            return fullPath;
        }

        public int GetLastIteration(string workingDir)
        {
            var sortedWorkingDir = Directory.GetDirectories(caseDirectory);
            Array.Sort(sortedWorkingDir, new Utilities.NumericComparer());

            var lastIteration = Path.GetDirectoryName(sortedWorkingDir[sortedWorkingDir.Length - 1]);
            int lastIterationInt = int.Parse(lastIteration);

            return lastIterationInt;
        }


     


    }

}
