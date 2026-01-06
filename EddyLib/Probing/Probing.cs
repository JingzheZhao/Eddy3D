using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EddyLib
{
    public class Probing
    {
        public GH_Number[] ResultScalar;

        public GH_Vector[] ResultVec;

        public int correspondingWindDir;

        private readonly List<Point3d> listOfPoints;

        private readonly string caseDirectory;

        private readonly string baseWorkingDirectory;

        public readonly string probingFilePath;

        public Probing(List<Point3d> ListOfPoints, string caseDirectory, string baseWorkingDirectory, OFField ofField, OFResult RES, bool rerun, string currWindDir = "")
        {
            listOfPoints = ListOfPoints;

            this.caseDirectory = caseDirectory;
            this.baseWorkingDirectory = baseWorkingDirectory;

            this.probingFilePath = GetPathToProbedResults(caseDirectory, ofField, RES);

            if (string.IsNullOrEmpty(probingFilePath)) return;

            // We gather the probes in both the root folder and in each individual case
            //string PostProcessDirCurrCase = caseDirectory + @"\postProcessing\";

            string postProcessDir = ProbePathHelper.EnsurePostProcessingDir(baseWorkingDirectory);
            string binPath = ProbePathHelper.BuildProbeBinaryPath(postProcessDir, currWindDir, ofField.ProbeName, ofField.FieldName);

            if (!rerun && File.Exists(binPath))
            {
                LoadProbedResultFromBinary(ofField, binPath);
            }
            else
            {
                ParseFromOFResult(ofField);
                WriteProbedResultToBinary(ofField, binPath);
            }
        }

        private void ParseFromOFResult(OFField ofField)
        {
            //Scalar
            if (ofField.FieldType == fieldType.scalar)
            {
                ResultScalar = ProbeParsing.ParseScalars(probingFilePath, listOfPoints.Count);
            }

            //Vector
            else
            {
                ResultVec = ProbeParsing.ParseVectors(probingFilePath, listOfPoints.Count);
            }
        }

        private void WriteProbedResultToBinary(OFField ofField, string binPath)
        {
            if (ofField.FieldType == fieldType.scalar)
            {
                ProbeBinaryIO.WriteScalars(binPath, ResultScalar);
            }
            if (ofField.FieldType == fieldType.vector)
            {
                ProbeBinaryIO.WriteVectors(binPath, ResultVec);
            }
        }

        private void LoadProbedResultFromBinary(OFField ofField, string binPath)
        {
            if (ofField.FieldType == fieldType.scalar)
            {
                ResultScalar = ProbeBinaryIO.LoadScalars(binPath);
            }
            if (ofField.FieldType == fieldType.vector)
            {
                ResultVec = ProbeBinaryIO.LoadVectors(binPath);
            }
        }

        public static string GetPathToProbedResults(string workingDirectory, OFField ofField, OFResult RES)
        {
            int lastIter = GetLatestTime(workingDirectory, RES, ofField);
            string fullPath = ProbePathHelper.BuildProbeFilePath(workingDirectory, ofField.ProbeName, ofField.FieldName, lastIter);

            if (!File.Exists(fullPath)) return string.Empty;

            //"C:\test\0\postProcessing\test3\303\p"

            return fullPath;
        }

        public static int GetLatestTime(string workingDirectory, OFResult RES, OFField ofField)
        {
            return ProbeTimeHelper.GetLatestIteration(workingDirectory, null);
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

        public static string ReformatIS(int IS)
        {
            //interpolationScheme.AddNamedValue("cell", 0);
            //interpolationScheme.AddNamedValue("cellPoint", 1);
            //interpolationScheme.AddNamedValue("cellPointFace", 2);
            //interpolationScheme.AddNamedValue("pointMVC", 3);
            //interpolationScheme.AddNamedValue("cellPatchConstrained", 4);

            switch (IS)
            {
                case 1:
                    return "cell";

                case 2:
                    return "cellPoint";

                case 3:
                    return "cellPointFace";

                case 4:
                    return "pointMVC";

                case 5:
                    return "cellPatchConstrained";

                default: return "cell";
            }
        }
    }
}
