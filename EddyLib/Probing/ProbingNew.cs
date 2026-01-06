using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System.Collections.Generic;
using System.IO;

namespace EddyLib
{
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
            this.currWindDir = currWindDir;
            probingFilePath = GetPathToProbedResults(caseDirectory, ofField, RES);

            if (string.IsNullOrEmpty(probingFilePath)) return;

            ParseFromOFResult(ofField);

            //WriteProbedResultToCSV(ofField);
            WriteProbedResultToBinary(ofField);
        }

        private void ParseFromOFResult(OFFieldNew ofField)
        {
            if (ofField.FieldType == fieldType.scalar)
            {
                ResultScalar = ProbeParsing.ParseScalars(probingFilePath, listOfPoints.Count);
            }
            else
            {
                ResultVec = ProbeParsing.ParseVectors(probingFilePath, listOfPoints.Count);
            }
        }

        public static int GetLatestTime(string workingDirectory, OFResult RES)
        {
            int? writeInterval = RES?.RunSettings?.iter > 0 ? RES.RunSettings.iter : (int?)null;
            return ProbeTimeHelper.GetLatestIteration(workingDirectory, writeInterval);
        }

        public static string GetPathToProbedResults(string workingDirectory, OFFieldNew ofField, OFResult RES)
        {
            int lastIter = GetLatestTime(workingDirectory, RES);
            string fullPath = ProbePathHelper.BuildProbeFilePath(workingDirectory, ofField.ProbeName, ofField.FieldName, lastIter);

            if (!File.Exists(fullPath)) return string.Empty;

            //"C:\test\0\postProcessing\test3\303\p"

            return fullPath;
        }

        private void WriteProbedResultToBinary(OFFieldNew ofField)
        {
            // We gather the probes in both the root folder and in each individual case
            string postProcessDir = ProbePathHelper.EnsurePostProcessingDir(baseWorkingDirectory);
            string outPath = ProbePathHelper.BuildProbeBinaryPath(postProcessDir, currWindDir.ToString(), ofField.ProbeName, ofField.FieldName);

            if (ofField.FieldType == fieldType.scalar)
            {
                ProbeBinaryIO.WriteScalars(outPath, ResultScalar);
            }
            if (ofField.FieldType == fieldType.vector)
            {
                ProbeBinaryIO.WriteVectors(outPath, ResultVec);
            }
        }
    }
}
