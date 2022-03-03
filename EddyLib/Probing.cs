using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EddyLib
{
    public enum fieldType
    {
        vector,

        scalar
    }

    public enum field
    {
        U, p, cp_coeff, epsilon, omega, k, nut, phi, aoa
    }

    public class OFFieldNew
    {
        public string FieldName { get; set; }

        public string ProbeName { get; set; }

        public field Field { get; set; }

        public fieldType FieldType { get; set; }

        public OFFieldNew(string probeName, field field)
        {
            Field = field;

            if (this.Field == field.U)
            {
                this.FieldType = fieldType.vector;
            }
            else
            {
                this.FieldType = fieldType.scalar;
            }

            // User given name
            ProbeName = probeName;

            // OF internal name

            if (this.Field == field.cp_coeff)

                this.FieldName = "total(p)_coeff";
            else
            {
                this.FieldName = field.ToString();
            }
        }
    }

    public class ProbingNew
    {
        public GH_Number[] ResultScalar;

        public GH_Vector[] ResultVec;

        public int correspondingWindDir;

        private readonly List<Point3d> listOfPoints;

        private readonly string caseDirectory;

        private readonly string baseWorkingDirectory;

        private readonly int currWindDir;

        public readonly string probingFilePath;

        public int[] IndecesOfExtremeProbes { get; set; }

        public ProbingNew(List<Point3d> ListOfPoints, string caseDirectory, string baseWorkingDirectory, OFFieldNew ofField, int currWindDir, OFResult RES)
        {
            listOfPoints = ListOfPoints;

            this.caseDirectory = caseDirectory;
            this.baseWorkingDirectory = baseWorkingDirectory;
            string probingFilePath = GetPathToProbedResults(caseDirectory, ofField, RES);

            if (probingFilePath == "") return;

            //Scalar
            if (ofField.FieldType == fieldType.scalar)
            {
                ParsingScalars(listOfPoints, probingFilePath);
            }

            //Vector
            else
            {
                ParsingVectors(listOfPoints, probingFilePath);
            }
            this.currWindDir = currWindDir;

            WriteProbedResultToCSV(ofField);
        }

        public static int GetLatestTime(string workingDirectory, OFResult RES)
        {
            var DirNames = Directory.GetDirectories(workingDirectory);

            var dirlist = new List<string>();
            foreach (string s in DirNames)
            {
                dirlist.Add(new DirectoryInfo(s).Name);
            }

            var numberList = new List<int>();
            int number;

            foreach (var name in dirlist)
            {
                Match m = Regex.Match(name, "\\d+"); // this gets the number at beginning of dirname
                var isNumber = Int32.TryParse(m.ToString(), out number);

                if (isNumber)
                    numberList.Add(number);
            }

            var highest = numberList.OrderByDescending(x => x).FirstOrDefault();

            // When we are probing cps, we need to make sure that the field has been written into the direction even if the case just recently converged and the writeTime wasn't hit yet. In those cases, we probe from the second-to-last directory.
            // double check this later
            if (highest % RES.RunSettings.iter != 0)
                highest = numberList.OrderByDescending(x => x).ElementAtOrDefault(1);

            return highest;
        }

        public static string GetPathToProbedResults(string workingDirectory, OFFieldNew ofField, OFResult RES)
        {
            int lastIter = GetLatestTime(workingDirectory, RES);
            string fullPath = workingDirectory + @"\postProcessing\" + @"\" + ofField.ProbeName + @"\" + lastIter.ToString() + @"\" + ofField.FieldName;

            if (!File.Exists(fullPath)) return "";

            //"C:\test\0\postProcessing\test3\303\p"

            return fullPath;
        }

        private void WriteProbedResultToCSV(OFFieldNew ofField)
        {
            // We gather the probes in both the root folder and in each individual case
            string PostProcessDirCurrCase = caseDirectory + @"\postProcessing\";
            string PostProcessDirBaseCase = baseWorkingDirectory + @"\postProcessing\";

            if (!Directory.Exists(PostProcessDirBaseCase))
            {
                Directory.CreateDirectory(PostProcessDirBaseCase);
            }
            StringBuilder sb = new StringBuilder();
            if (ofField.FieldType == fieldType.scalar)
            {
                foreach (GH_Number i in ResultScalar)
                {
                    sb.AppendLine(i.ToString());
                }
            }
            if (ofField.FieldType == fieldType.vector)
            {
                foreach (GH_Vector i in ResultVec)
                {
                    sb.AppendLine(i.ToString());
                }
            }
            File.WriteAllText(PostProcessDirBaseCase + currWindDir + "_" + ofField.ProbeName + "_" + ofField.FieldName + ".csv", sb.ToString());
        }

        private void ParsingScalars(List<Point3d> listOfPoints, string fullPath)
        {
            int counterPoints = listOfPoints.Count;

            StringBuilder sb = new StringBuilder();

            ResultScalar = new GH_Number[counterPoints];
            string lastLine = File.ReadLines(fullPath).Where(line => line != "").Last();

            for (int i = 0; i < counterPoints; i++)
            {
                var temp = double.Parse(lastLine.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[i + 1]);
                var target = new GH_Number(0);
                var conversion = GH_Convert.ToGHNumber(temp, GH_Conversion.Both, ref target);
                ResultScalar[i] = target;
            }
        }

        private void ParsingVectors(List<Point3d> listOfPoints, string fullPath)
        {
            int counterPoints = listOfPoints.Count;

            ResultVec = new GH_Vector[counterPoints];
            string lastLine = File.ReadLines(fullPath).Last();
            string replacedString = System.Text.RegularExpressions.Regex.Replace(lastLine, "[()]", "", RegexOptions.Compiled);
            string[] abc = replacedString.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            int counter = 1;
            for (int i = 0; i < counterPoints; i++)
            {
                var temp = (new Vector3d(double.Parse(abc[counter]), double.Parse(abc[counter + 1]), double.Parse(abc[counter + 2])));
                var target = new GH_Vector();
                GH_Convert.ToGHVector(temp, GH_Conversion.Both, ref target);
                ResultVec[i] = target;
                counter += 3;
            }
        }
    }

    public class OFField
    {
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
                FieldType = fieldType.scalar;
            }
            else if (fieldName == "p")
            {
                FieldType = fieldType.scalar;
            }
            else if (fieldName == "epsilon")
            {
                FieldType = fieldType.scalar;
            }
            else if (fieldName == "omega")
            {
                FieldType = fieldType.scalar;
            }
            else if (fieldName == "k")
            {
                FieldType = fieldType.scalar;
            }
            else if (fieldName == "nut")
            {
                FieldType = fieldType.scalar;
            }
            else if (fieldName == "phi")
            {
                FieldType = fieldType.scalar;
            }
            else if (fieldName == "aoa")
            {
                FieldType = fieldType.scalar;
            }
        }

        public static string ReformatOFFields(int OFFieldInt)
        {
            string ofField;

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
            else if (OFFieldInt == 7)
            {
                ofField = "phi";

                //fieldType = 1;
            }
            else
            {
                ofField = "aoa";
            }
            return ofField;
        }
    }

    public class Probing
    {
        public GH_Number[] ResultScalar;

        public GH_Vector[] ResultVec;

        public int correspondingWindDir;

        private readonly List<Point3d> listOfPoints;

        private readonly string caseDirectory;

        private readonly string baseWorkingDirectory;

        private readonly int currWindDir;

        public readonly string probingFilePath;

        // Todo: Implement this
        //public int[] IndexOfExtremeProbes;

        public Probing(List<Point3d> ListOfPoints, string caseDirectory, string baseWorkingDirectory, OFField ofField, int currWindDir, OFResult RES)
        {
            listOfPoints = ListOfPoints;

            this.caseDirectory = caseDirectory;
            this.baseWorkingDirectory = baseWorkingDirectory;
            string fullPath = GetPathToProbedResults(caseDirectory, ofField, RES);

            if (fullPath == "") return;

            //Scalar
            if (ofField.FieldType == fieldType.scalar)
            {
                ParsingScalars(listOfPoints, fullPath);
            }

            //Vector
            if (ofField.FieldType == fieldType.vector)
            {
                ParsingVectors(listOfPoints, fullPath);
            }
            this.currWindDir = currWindDir;
            this.probingFilePath = GetPathToProbedResults(caseDirectory, ofField, RES);
            WriteProbedResultToCSV(ofField);
        }

        private void WriteProbedResultToCSV(OFField ofField)
        {
            // We gather the probes in both the root folder and in each individual case
            string PostProcessDirCurrCase = caseDirectory + @"\postProcessing\";
            string PostProcessDirBaseCase = baseWorkingDirectory + @"\postProcessing\";

            if (!Directory.Exists(PostProcessDirBaseCase))
            {
                Directory.CreateDirectory(PostProcessDirBaseCase);
            }

            if (ofField.FieldType == fieldType.scalar)
            {
                StringBuilder sb = new StringBuilder();
                foreach (GH_Number i in ResultScalar)
                {
                    sb.AppendLine(i.ToString());
                }
                File.WriteAllText(PostProcessDirCurrCase + ofField.ProbeName + ".csv", sb.ToString());
                File.WriteAllText(PostProcessDirBaseCase + currWindDir + "_" + ofField.ProbeName + ".csv", sb.ToString());
            }
            if (ofField.FieldType == fieldType.vector)
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

        private void ParsingScalars(List<Point3d> listOfPoints, string fullPath)
        {
            int counterPoints = listOfPoints.Count;

            StringBuilder sb = new StringBuilder();

            ResultScalar = new GH_Number[counterPoints];
            string lastLine = File.ReadLines(fullPath).Where(line => line != "").Last();

            for (int i = 0; i < counterPoints; i++)
            {
                var temp = double.Parse(lastLine.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[i + 1]);
                var target = new GH_Number(0);
                var conversion = GH_Convert.ToGHNumber(temp, GH_Conversion.Both, ref target);
                ResultScalar[i] = target;
            }
        }

        private void ParsingVectors(List<Point3d> listOfPoints, string fullPath)
        {
            int counterPoints = listOfPoints.Count;

            ResultVec = new GH_Vector[counterPoints];
            string lastLine = File.ReadLines(fullPath).Last();
            string replacedString = System.Text.RegularExpressions.Regex.Replace(lastLine, "[()]", "", RegexOptions.Compiled);
            string[] abc = replacedString.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            int counter = 1;
            for (int i = 0; i < counterPoints; i++)
            {
                var temp = (new Vector3d(double.Parse(abc[counter]), double.Parse(abc[counter + 1]), double.Parse(abc[counter + 2])));
                var target = new GH_Vector();
                GH_Convert.ToGHVector(temp, GH_Conversion.Both, ref target);
                ResultVec[i] = target;
                counter += 3;
            }
        }

        public static string GetPathToProbedResults(string workingDirectory, OFField ofField, OFResult RES)
        {
            int lastIter = GetLatestTime(workingDirectory, RES, ofField);
            string fullPath = workingDirectory + @"\postProcessing\" + @"\" + ofField.ProbeName + @"\" + lastIter.ToString() + @"\" + ofField.FieldName;

            if (!File.Exists(fullPath)) return "";

            //"C:\test\0\postProcessing\test3\303\p"

            return fullPath;
        }

        public static int GetLatestTime(string workingDirectory, OFResult RES, OFField ofField)
        {
            var DirNames = Directory.GetDirectories(workingDirectory);

            var dirlist = new List<string>();
            foreach (string s in DirNames)
            {
                dirlist.Add(new DirectoryInfo(s).Name);
            }

            var numberList = new List<int>();
            int number;

            foreach (var name in dirlist)
            {
                Match m = Regex.Match(name, "\\d+"); // this gets the number at beginning of dirname
                var isNumber = Int32.TryParse(m.ToString(), out number);

                if (isNumber)
                    numberList.Add(number);
            }

            var highest = numberList.OrderByDescending(x => x).FirstOrDefault();

            // When we are probing cps, we need to make sure that the field has been written into the direction even if the case just recently converged and the writeTime wasn't hit yet. In those cases, we probe from the second-to-last directory.

            if (highest % RES.RunSettings.iter != 0 && ofField.FieldName == "total(p)_coeff")
                highest = numberList.OrderByDescending(x => x).ElementAtOrDefault(1);

            return highest;
        }

        public static int[] ReturnProbeIndicesOutsideDomain(List<GH_Vector> x)
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