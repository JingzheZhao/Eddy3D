using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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
    public class Probes
    {

        public double[] ResultNum;
        public Vector3d[] ResultVec;
        public string valueString;


        private readonly List<Point3d> listOfPoints;

        private readonly string caseDirectory;

        public Probes(List<Point3d> ListOfPoints, string caseDirectory, OFField ofField)
        {
            listOfPoints = ListOfPoints;

            this.caseDirectory = caseDirectory;
            string fullPath = GetIterationPathToProbedResults(caseDirectory, ofField);

            //Number       
            if (ofField.FieldType == OFField.fieldType.number)
            {
                ParsingNumbers(listOfPoints, caseDirectory, fullPath);
            }
            //Vector
            if (ofField.FieldType == OFField.fieldType.vector)
            {
                ParsingVectors(listOfPoints, caseDirectory, fullPath);
            }
            WriteProbedResultToCSV(ofField);
        }


        private void WriteProbedResultToCSV(OFField ofField)
        {

            string PostProcessingDirectory = caseDirectory + @"\postProcessing\";
            if (ofField.FieldType == OFField.fieldType.number)
            {
                StringBuilder sb = new StringBuilder();
                foreach (double i in ResultNum)
                {
                    sb.AppendLine(i.ToString());
                }
                File.WriteAllText(PostProcessingDirectory + ofField.ProbeName + ".csv", sb.ToString());
            }
            if (ofField.FieldType == OFField.fieldType.vector)
            {
                StringBuilder sb = new StringBuilder();
                foreach (Vector3d i in ResultVec)
                {
                    sb.AppendLine(i.ToString());
                }
                File.WriteAllText(PostProcessingDirectory + ofField.ProbeName + ".csv", sb.ToString());
            }
        }

        private void ParsingNumbers(List<Point3d> listOfPoints, string workingDirectory, string fullPath)
        {

            int counterPoints = listOfPoints.Count;

            StringBuilder sb = new StringBuilder();

            ResultNum = new double[counterPoints];
            string lastLine = File.ReadLines(fullPath).Where(line => line != "").Last();

            for (int i = 0; i < counterPoints; i++)
            {
                ResultNum[i] = double.Parse(lastLine.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[i + 1]); //this workes 
                sb.AppendLine(ResultNum[i].ToString());
            }
            valueString = sb.ToString();

        }

        private void ParsingVectors(List<Point3d> listOfPoints, string workingDirectory, string fullPath)
        {

            int counterPoints = listOfPoints.Count;

            StringBuilder sb = new StringBuilder();

            ResultVec = new Vector3d[counterPoints];
            string lastLine = File.ReadLines(fullPath).Last();
            string replacedString = System.Text.RegularExpressions.Regex.Replace(lastLine, "[()]", "", RegexOptions.Compiled);
            string[] abc = replacedString.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            int counter = 1;
            for (int i = 0; i < counterPoints; i++)
            {
                ResultVec[i] = (new Vector3d(double.Parse(abc[counter]), double.Parse(abc[counter + 1]), double.Parse(abc[counter + 2])));
                sb.AppendLine(ResultVec[i].ToString());
                counter += 3;
            }
            valueString = sb.ToString();
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
