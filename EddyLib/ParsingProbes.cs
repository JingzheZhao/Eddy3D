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

        public double[] numberValues;
        public Vector3d[] vectorValues;        
        public string valueString;


        private readonly List<Point3d> listOfPoints;
        
        private readonly string caseDirectory;


        public ParsingProbes(List<Point3d> ListOfPoints, string enumeratedProbeName, string caseDirectory, string OFfield, int fieldtype)
        {
            listOfPoints = ListOfPoints;
            
            this.caseDirectory = caseDirectory;
            //Number
            if (fieldtype == 0)
            {
                ParsingNumbers(listOfPoints, enumeratedProbeName, this.caseDirectory, OFfield);
            }
            //Vector
            if (fieldtype == 1)
            {
                ParsingVectors(listOfPoints, enumeratedProbeName, this.caseDirectory, OFfield);
            }
            WriteToCSV(fieldtype, enumeratedProbeName);
        }


        private void WriteToCSV(int fieldtype, string enumeratedProbeName)
        {
            string PostProcessingDirectory = caseDirectory + @"\postProcessing\";
            if (fieldtype == 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach (double i in this.numberValues)
                {
                    sb.AppendLine(i.ToString());
                }
                File.WriteAllText(PostProcessingDirectory + enumeratedProbeName + ".csv", sb.ToString());
            }
            if (fieldtype == 1)
            {
                StringBuilder sb = new StringBuilder();
                foreach (Vector3d i in this.vectorValues)
                {
                    sb.AppendLine(i.ToString());
                }
                File.WriteAllText(PostProcessingDirectory + enumeratedProbeName + ".csv", sb.ToString());
            }
            


        }


        private void ParsingNumbers(List<Point3d> listOfPoints, string enumeratedProbeName, string workingDirectory, string OFfield)
        {

            string fullPath = GetLastProcProssDir(enumeratedProbeName, workingDirectory, OFfield);

            var counterPoints = listOfPoints.Count;

            StringBuilder sb = new StringBuilder();


            this.numberValues = new double[counterPoints];
            var lastLine = File.ReadLines(fullPath).Where(line => line != "").Last();

            for (int i = 0; i < counterPoints; i++)
            {
                this.numberValues[i] = double.Parse(lastLine.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[i + 1]); //this workes 
                sb.AppendLine(this.numberValues[i].ToString());
            }
            this.valueString = sb.ToString();


        }

        private void ParsingVectors(List<Point3d> listOfPoints, string pointName, string workingDirectory, string OFfield)
        {

            string fullPath = GetLastProcProssDir(pointName, workingDirectory, OFfield);

            var counterPoints = listOfPoints.Count;

            StringBuilder sb = new StringBuilder();


            this.vectorValues = new Vector3d[counterPoints];
            var lastLine = File.ReadLines(fullPath).Last();
            string replacedString = System.Text.RegularExpressions.Regex.Replace(lastLine, "[()]", "", RegexOptions.Compiled);
            string[] abc = replacedString.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            int counter = 1;
            for (int i = 0; i < counterPoints; i++)
            {
                this.vectorValues[i] = (new Vector3d(double.Parse(abc[counter]), double.Parse(abc[counter + 1]), double.Parse(abc[counter + 2])));
                sb.AppendLine(this.vectorValues[i].ToString());
                counter += 3;
            }
            this.valueString = sb.ToString();


        }

        public static string GetLastProcProssDir(string enumeratedProbeName, string workingDirectory, string field)
        {
            
            string PostProcessingDirectory = workingDirectory + @"\postProcessing\";


            //replace this with input
            string basePath = PostProcessingDirectory + enumeratedProbeName;

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

        


     


    }

}
