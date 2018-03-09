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

namespace Eddy
{
    public class ParsingValues
    {

        public double[] cpValues;
        public Vector3d[] uValues;
        public string valueString;
        

        private List<Point3d> listOfPoints;
        private string pointName;
        private string workingDirectory;


        public ParsingValues(List<Point3d> ListOfPoints, string PointName, string WorkingDirectory) {                       
            listOfPoints = ListOfPoints;
            pointName = PointName;
            workingDirectory = WorkingDirectory;
            if (pointName == "cp_Probes")
            {
                ParsingNumbers(listOfPoints, pointName, workingDirectory);
            }
            else
            {
                ParsingVectors(listOfPoints, pointName, workingDirectory);
            }
            writeToCSV();
        }
             

        private void writeToCSV()
        {
             string postProcessingDirectory = workingDirectory + @"\postProcessing\";
            if (pointName == "cp_Probes")
            {
                File.WriteAllText(postProcessingDirectory + pointName + ".csv", this.cpValues.ToString());
            }
            else
            {
                File.WriteAllText(postProcessingDirectory + pointName + ".csv", this.uValues.ToString());
            }
             

        }


        private void ParsingNumbers(List<Point3d> listOfPoints, string pointName, string workingDirectory)
        {
            
            string fullPath = getLastProcProssDir(listOfPoints, pointName, workingDirectory);

            var counterPoints = listOfPoints.Count;
            
            StringBuilder sb = new StringBuilder();
                      

                this.cpValues = new double[counterPoints];
                var lastLine = File.ReadLines(fullPath).Last();

                for (int i = 1; i < counterPoints; i++)
                {
                    this.cpValues[i] = double.Parse(lastLine.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[i]); //this workes
                    sb.AppendLine(this.cpValues[i].ToString());
                }
                this.valueString = sb.ToString();        
        

        }

        private void ParsingVectors(List<Point3d> listOfPoints, string pointName, string workingDirectory)
        {
            
            string fullPath = getLastProcProssDir(listOfPoints, pointName, workingDirectory);

            var counterPoints = listOfPoints.Count;
            
            StringBuilder sb = new StringBuilder();

        
                this.uValues = new Vector3d[counterPoints];
                var lastLine = File.ReadLines(fullPath).Last();
                string replacedString = System.Text.RegularExpressions.Regex.Replace(lastLine, "[()]", "", RegexOptions.Compiled);
            string[] abc = replacedString.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            int counter = 1;
            for (int i = 0; i < counterPoints; i++)
                {
                   this.uValues[i] = (new Vector3d(double.Parse(abc[counter]), double.Parse(abc[counter+1]), double.Parse(abc[counter + 2])));
                   sb.AppendLine(this.uValues[i].ToString());
                counter+=3;
                }
            this.valueString = sb.ToString();
                

        }

            public string getLastProcProssDir(List<Point3d> listOfPoints, string pointName, string workingDirectory)
            {
                var counterPoints = listOfPoints.Count;
                string postProcessingDirectory = workingDirectory + @"\postProcessing\";
                

                //replace this with input
                string basePath = postProcessingDirectory + pointName;
                
                //string[] filePathResults = new string[counterPoints];
                var directoriesBasePath = Directory.GetDirectories(basePath);
                Array.Sort(directoriesBasePath, new Utilities.NumericComparer());

                var latestTimedirectoriesBasePath = directoriesBasePath[directoriesBasePath.Length-1];
                string latestTime = Path.GetFileName(latestTimedirectoriesBasePath);

                string fullPath = basePath + @"\" + latestTime + @"\" + "U";
                return fullPath;
            }


        }

    }
