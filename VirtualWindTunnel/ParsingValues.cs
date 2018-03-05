using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.IO;
using Rhino.Geometry;

namespace Eddy
{
    public class ParsingValues
    {
        public ParsingValues(List<Point3d> listOfPoints, string pointName, string workingDirectory)
        {
            
            string fullPath = getLastProcProssDir(listOfPoints, pointName, workingDirectory);

            var counterPoints = listOfPoints.Count;
            
            StringBuilder sb = new StringBuilder();

            if (pointName == "cp_Probes")
            {

                var values = new double[counterPoints];
                var lastLine = File.ReadLines(fullPath).Last();

                for (int i = 1; i < counterPoints; i++)
                {
                    values[i] = double.Parse(lastLine.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[i]); //this workes
                }
            }
            else
            {
                var values = new Vector3d[counterPoints];
                var lastLine = File.ReadLines(fullPath).Last();
                string replacedString = System.Text.RegularExpressions.Regex.Replace(lastLine, "[()]", "", RegexOptions.Compiled);

                for (int i = 1; i < 3 * counterPoints; i += 3)
                {
                    values[i - 1] = (new Vector3d(double.Parse(replacedString.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[i]), double.Parse(replacedString.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[i + 1]), double.Parse(replacedString.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[i + 2])));
                    sb.AppendLine(values[i - 1].ToString());
                }
            }

        }

            public string getLastProcProssDir(List<Point3d> listOfPoints, string pointName, string workingDirectory)
            {
                var counterPoints = listOfPoints.Count;
                string postProcessingDirectory = workingDirectory + @"\postProcessing\";
                string[] dir = Directory.GetDirectories(postProcessingDirectory);

                //replace this with input
                string basePath = postProcessingDirectory + pointName;
                int counterIter = Directory.GetDirectories(basePath).Length;

                string[] filePathResults = new string[counterPoints];
                string[] directoriesBasePath = Directory.GetDirectories(dir[0]);

                var latestTimedirectoriesBasePath = directoriesBasePath[directoriesBasePath.Length - 1];
                string latestTime = Path.GetFileName(latestTimedirectoriesBasePath);

                string fullPath = basePath + @"\" + latestTime + @"\" + "U";
                return fullPath;
            }


        }
    }
