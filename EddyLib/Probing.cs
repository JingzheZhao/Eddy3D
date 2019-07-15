using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace EddyLib
{
    public class OFField
    {
        public enum fieldType
        {
            vector,
            number
        }

        public string FieldName { get; set; }
        public string ProbeName { get; set; }
        public fieldType FieldType { get; set; }

        public OFField(string fieldName, string probeName)
        {
            Setup(fieldName, probeName);
        }

        private void Setup(string fieldName, string probeName)
        {
            //param.AddNamedValue("U", 0);
            //param.AddNamedValue("total(p)_coeff", 1);
            //param.AddNamedValue("p", 2);
            //param.AddNamedValue("epsilon", 3);
            //param.AddNamedValue("omega", 4);
            //param.AddNamedValue("k", 5);
            //param.AddNamedValue("nut", 6);
            //param.AddNamedValue("phi", 7);

            FieldName = fieldName;
            ProbeName = probeName;

            if (fieldName == "U")
            {
                FieldType = fieldType.vector;
            }
            else if (fieldName == "total(p)_coeff")
            {
                FieldType = fieldType.number;
            }
            else if (fieldName == "p")
            {
                FieldType = fieldType.number;
            }
            else if (fieldName == "epsilon")
            {
                FieldType = fieldType.number;
            }
            else if (fieldName == "omega")
            {
                FieldType = fieldType.number;
            }
            else if (fieldName == "k")
            {
                FieldType = fieldType.number;
            }
            else if (fieldName == "nut")
            {
                FieldType = fieldType.number;
            }
            else if (fieldName == "phi")
            {
                FieldType = fieldType.number;
            }
        }

        public static string ReformatOFFields(int OFFieldInt)
        {
            string ofField = "";
            //fieldType = 0;
            if (OFFieldInt == 0)
            {
                //fieldType = 1;
                ofField = "U";
            }
            else if (OFFieldInt == 1)
            {
                ofField = "total(p)_coeff";
                //fieldType = 0;
            }
            else if (OFFieldInt == 2)
            {
                ofField = "p";
                //fieldType = 0;
            }
            else if (OFFieldInt == 3)
            {
                ofField = "epsilon";
                //fieldType = 0;
            }
            else if (OFFieldInt == 4)
            {
                ofField = "omega";
                //fieldType = 0;
            }
            else if (OFFieldInt == 5)
            {
                ofField = "k";
                //fieldType = 0;
            }
            else if (OFFieldInt == 6)
            {
                ofField = "nut";
                //fieldType = 0;
            }
            else
            {
                ofField = "phi";
                //fieldType = 1;
            }
            return ofField;
        }
    }

    public class Probing
    {
        public GH_Number[] ResultNum;
        public GH_Vector[] ResultVec;

        //public string valueString;
        public int correspondingWindDir;

        private readonly List<Point3d> listOfPoints;

        private readonly string caseDirectory;
        private readonly string baseWorkingDirectory;
        private readonly int currWindDir;

        // Todo: Implement this
        //public int[] IndexOfExtremeProbes;

        public Probing(List<Point3d> ListOfPoints, string caseDirectory, string baseWorkingDirectory, OFField ofField, int currWindDir)
        {
            listOfPoints = ListOfPoints;

            this.caseDirectory = caseDirectory;
            this.baseWorkingDirectory = baseWorkingDirectory;
            string fullPath = GetIterationPathToProbedResults(caseDirectory, ofField);

            //Number
            if (ofField.FieldType == OFField.fieldType.number)
            {
                ParsingNumbers(listOfPoints, fullPath);
            }
            //Vector
            if (ofField.FieldType == OFField.fieldType.vector)
            {
                ParsingVectors(listOfPoints, fullPath);
            }
            this.currWindDir = currWindDir;
            WriteProbedResultToCSV(ofField);
        }

        private void WriteProbedResultToCSV(OFField ofField)
        {
            string PostProcessDirCurrCase = caseDirectory + @"\postProcessing\";
            string PostProcessDirBaseCase = baseWorkingDirectory + @"\postProcessing\";

            if (!Directory.Exists(PostProcessDirBaseCase))
            {
                Directory.CreateDirectory(PostProcessDirBaseCase);
            }

            if (ofField.FieldType == OFField.fieldType.number)
            {
                StringBuilder sb = new StringBuilder();
                foreach (GH_Number i in ResultNum)
                {
                    sb.AppendLine(i.ToString());
                }
                File.WriteAllText(PostProcessDirCurrCase + ofField.ProbeName + ".csv", sb.ToString());
                File.WriteAllText(PostProcessDirBaseCase + currWindDir + "_" + ofField.ProbeName + ".csv", sb.ToString());
            }
            if (ofField.FieldType == OFField.fieldType.vector)
            {
                StringBuilder sb = new StringBuilder();
                foreach (GH_Vector i in ResultVec)
                {
                    sb.AppendLine(i.ToString());
                }
                File.WriteAllText(PostProcessDirCurrCase + ofField.ProbeName + ".csv", sb.ToString());
                File.WriteAllText(PostProcessDirBaseCase + currWindDir + "_" + ofField.ProbeName + ".csv", sb.ToString());
            }
        }

        private void ParsingNumbers(List<Point3d> listOfPoints, string fullPath)
        {
            int counterPoints = listOfPoints.Count;

            StringBuilder sb = new StringBuilder();

            ResultNum = new GH_Number[counterPoints];
            string lastLine = File.ReadLines(fullPath).Where(line => line != "").Last();

            for (int i = 0; i < counterPoints; i++)
            {
                var temp = double.Parse(lastLine.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[i + 1]); //this workes
                var target = new GH_Number(0);
                var conversion = GH_Convert.ToGHNumber(temp, GH_Conversion.Both, ref target);
                ResultNum[i] = target;
                //sb.AppendLine(ResultNum[i].ToString());
            }
            //this.valueString = sb.ToString();
        }

        private void ParsingVectors(List<Point3d> listOfPoints, string fullPath)
        {
            int counterPoints = listOfPoints.Count;

            StringBuilder sb = new StringBuilder();

            ResultVec = new GH_Vector[counterPoints];
            string lastLine = File.ReadLines(fullPath).Last();
            string replacedString = System.Text.RegularExpressions.Regex.Replace(lastLine, "[()]", "", RegexOptions.Compiled);
            string[] abc = replacedString.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            int counter = 1;
            for (int i = 0; i < counterPoints; i++)
            {
                var temp = (new Vector3d(double.Parse(abc[counter]), double.Parse(abc[counter + 1]), double.Parse(abc[counter + 2])));
                var target = new GH_Vector();
                var conversion = GH_Convert.ToGHVector(temp, GH_Conversion.Both, ref target);
                ResultVec[i] = target;
                //sb.AppendLine(ResultVec[i].ToString());
                counter += 3;
            }
            //this.valueString = sb.ToString();
        }

        public static string GetIterationPathToProbedResults(string workingDirectory, OFField ofField)
        {
            // Here, the data has to be written already
            string PostProcessingDirectory = workingDirectory + @"\postProcessing\";

            //replace this with input
            string basePath = PostProcessingDirectory + ofField.ProbeName;

            //if (!Directory.Exists(basePath)){
            //    Directory.CreateDirectory(basePath);
            //}

            //string[] filePathResults = new string[counterPoints];
            string[] directoriesBasePath = Directory.GetDirectories(basePath);
            Array.Sort(directoriesBasePath, new Utilities.NumericComparer());

            string latestTimedirectoriesBasePath = directoriesBasePath[directoriesBasePath.Length - 1];
            string latestTime = Path.GetFileName(latestTimedirectoriesBasePath);

            string fullPath = basePath + @"\" + latestTime + @"\" + ofField.FieldName;

            return fullPath;
        }

        public string GetLastIterationPath(string workingDirectory)
        {
            string[] sortedWorkingDir = Directory.GetDirectories(workingDirectory);
            Array.Sort(sortedWorkingDir, new Utilities.NumericComparer());

            string lastIteration = sortedWorkingDir[sortedWorkingDir.Length - 1];
            string latestTime = Path.GetDirectoryName(lastIteration);

            string fullPath = workingDirectory + @"\" + latestTime;
            return fullPath;
        }

        //public static double[] FilterExtremeCPs(double[] inputList)
        //{
        //    double[] outputList = new double[inputList.Length];

        //    for (int i = 0; i < inputList.Length; i++)
        //    {
        //        if (inputList[i] < -1)
        //        {
        //            outputList[i] = -1;
        //        }
        //        else if (inputList[i] > 1)
        //        {
        //            outputList[i] = 1;
        //        }
        //        else
        //        {
        //            outputList[i] = inputList[i];
        //        }
        //    }
        //    return outputList;
        //}

        public static int[] ReturnIndexOfExtremeProbes(List<GH_Vector> x)
        {
            var vecLengths = new List<double>();

            foreach (GH_Vector v in x)
            {
                vecLengths.Add(v.Value.Length);
            }
            return Enumerable.Range(0, vecLengths.Count).Where(i => vecLengths[i] > 10000).ToArray();
        }

        public static double[] FilterExtremeProbingValues(double[] inputList)
        {
            double[] outputList = new double[inputList.Length];

            for (int i = 0; i < inputList.Length; i++)
            {
                if (inputList[i] < -10000)
                {
                    outputList[i] = 0;
                }
                else if (inputList[i] > 10000)
                {
                    outputList[i] = 0;
                }
                else
                {
                    outputList[i] = inputList[i];
                }
            }
            return outputList;
        }

        public static Vector3d[] FilterExtremeVectorLengths(Vector3d[] inputList)
        {
            List<Vector3d> outputList = new List<Vector3d>();

            for (int i = 0; i < inputList.Length; i++)
            {
                if (inputList[i].Length < 1000)
                {
                    outputList.Add(inputList[i]);
                }
            }

            return outputList.ToArray();
        }
    }
}