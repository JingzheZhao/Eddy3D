using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System.Collections.Generic;
using System.IO;

namespace EddyLib
{
    /// <summary>
    /// Handles probing OpenFOAM simulation results using the new field enum.
    /// </summary>
    public class ProbingNew
    {
        #region Properties

        /// <summary>Probed scalar results.</summary>
        public GH_Number[] ResultScalar { get; private set; }

        /// <summary>Probed vector results.</summary>
        public GH_Vector[] ResultVec { get; private set; }

        /// <summary>Corresponding wind direction index.</summary>
        public int CorrespondingWindDir { get; set; }

        /// <summary>Path to the probing result file.</summary>
        public string ProbingFilePath { get; }

        /// <summary>Indices of probes with extreme values.</summary>
        public int[] IndicesOfExtremeProbes { get; set; }

        #endregion

        private readonly List<Point3d> probePoints;
        private readonly string caseDirectory;
        private readonly string baseWorkingDirectory;
        private readonly int currWindDir;

        /// <summary>
        /// Creates a probing session and loads results.
        /// </summary>
        public ProbingNew(List<Point3d> probePoints, string caseDirectory, string baseWorkingDirectory,
                          OFFieldNew ofField, int currWindDir, OFResult result)
        {
            this.probePoints = probePoints;
            this.caseDirectory = caseDirectory;
            this.baseWorkingDirectory = baseWorkingDirectory;
            this.currWindDir = currWindDir;

            ProbingFilePath = GetPathToProbedResults(caseDirectory, ofField, result);
            if (string.IsNullOrEmpty(ProbingFilePath)) return;

            ParseFromOpenFOAMResult(ofField);
            SaveToBinaryCache(ofField);
        }

        #region Private Methods

        private void ParseFromOpenFOAMResult(OFFieldNew ofField)
        {
            if (ofField.FieldType == fieldType.scalar)
            {
                ResultScalar = ProbeParsing.ParseScalars(ProbingFilePath, probePoints.Count);
            }
            else
            {
                ResultVec = ProbeParsing.ParseVectors(ProbingFilePath, probePoints.Count);
            }
        }

        private void SaveToBinaryCache(OFFieldNew ofField)
        {
            string postProcessDir = ProbePathHelper.EnsurePostProcessingDir(baseWorkingDirectory);
            string outPath = ProbePathHelper.BuildProbeBinaryPath(postProcessDir, currWindDir.ToString(), ofField.ProbeName, ofField.FieldName);

            if (ofField.FieldType == fieldType.scalar)
            {
                ProbeBinaryIO.WriteScalars(outPath, ResultScalar);
            }
            else if (ofField.FieldType == fieldType.vector)
            {
                ProbeBinaryIO.WriteVectors(outPath, ResultVec);
            }
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Gets the latest time step iteration number.
        /// </summary>
        public static int GetLatestTime(string workingDirectory, OFResult result)
        {
            return ProbeTimeHelper.GetLatestIteration(workingDirectory, null);
        }

        /// <summary>
        /// Gets the path to probed results for the latest time step.
        /// </summary>
        public static string GetPathToProbedResults(string workingDirectory, OFFieldNew ofField, OFResult result)
        {
            int lastIter = GetLatestTime(workingDirectory, result);
            string fullPath = ProbePathHelper.BuildProbeFilePath(workingDirectory, ofField.ProbeName, ofField.FieldName, lastIter);

            if (File.Exists(fullPath)) return fullPath;

            // Fallback to time 0 if latest iteration doesn't have the file (common in OpenFOAM 12)
            if (lastIter != 0)
            {
                string zeroPath = ProbePathHelper.BuildProbeFilePath(workingDirectory, ofField.ProbeName, ofField.FieldName, 0);
                if (File.Exists(zeroPath)) return zeroPath;
            }

            return string.Empty;
        }

        #endregion

        #region Backward Compatibility

        /// <summary>Legacy property - use IndicesOfExtremeProbes instead.</summary>
        public int[] IndecesOfExtremeProbes
        {
            get => IndicesOfExtremeProbes;
            set => IndicesOfExtremeProbes = value;
        }

        /// <summary>Legacy field - use CorrespondingWindDir instead.</summary>
        public int correspondingWindDir
        {
            get => CorrespondingWindDir;
            set => CorrespondingWindDir = value;
        }

        /// <summary>Legacy property - use ProbingFilePath instead.</summary>
        public string probingFilePath => ProbingFilePath;

        #endregion
    }
}
