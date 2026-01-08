using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EddyLib
{
    /// <summary>
    /// Handles probing OpenFOAM simulation results at specific points.
    /// </summary>
    public class Probing
    {
        #region Constants

        /// <summary>Threshold for detecting invalid probe values (outside domain).</summary>
        private const double InvalidValueThreshold = 10000;

        /// <summary>Threshold for filtering extreme vector lengths.</summary>
        private const double ExtremeVectorThreshold = 1000;

        #endregion

        #region Properties

        /// <summary>Probed scalar results.</summary>
        public GH_Number[] ResultScalar { get; private set; }

        /// <summary>Probed vector results.</summary>
        public GH_Vector[] ResultVec { get; private set; }

        /// <summary>Corresponding wind direction index.</summary>
        public int CorrespondingWindDir { get; set; }

        /// <summary>Path to the probing result file.</summary>
        public string ProbingFilePath { get; }

        #endregion

        private readonly List<Point3d> probePoints;
        private readonly string caseDirectory;
        private readonly string baseWorkingDirectory;

        /// <summary>
        /// Creates a probing session and loads results.
        /// </summary>
        /// <param name="probePoints">Points to probe at.</param>
        /// <param name="caseDirectory">Case directory path.</param>
        /// <param name="baseWorkingDirectory">Base working directory.</param>
        /// <param name="ofField">OpenFOAM field to probe.</param>
        /// <param name="result">Result settings.</param>
        /// <param name="rerun">Force re-parsing even if binary cache exists.</param>
        /// <param name="currWindDir">Current wind direction.</param>
        public Probing(List<Point3d> probePoints, string caseDirectory, string baseWorkingDirectory, 
                       OFField ofField, OFResult result, bool rerun, string currWindDir = "")
        {
            this.probePoints = probePoints;
            this.caseDirectory = caseDirectory;
            this.baseWorkingDirectory = baseWorkingDirectory;

            ProbingFilePath = GetPathToProbedResults(caseDirectory, ofField, result);
            if (string.IsNullOrEmpty(ProbingFilePath)) return;

            string postProcessDir = ProbePathHelper.EnsurePostProcessingDir(baseWorkingDirectory);
            string binPath = ProbePathHelper.BuildProbeBinaryPath(postProcessDir, currWindDir, ofField.ProbeName, ofField.FieldName);

            if (!rerun && File.Exists(binPath))
            {
                LoadFromBinaryCache(ofField, binPath);
            }
            else
            {
                ParseFromOpenFOAMResult(ofField);
                SaveToBinaryCache(ofField, binPath);
            }
        }

        #region Private Methods

        private void ParseFromOpenFOAMResult(OFField ofField)
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

        private void SaveToBinaryCache(OFField ofField, string binPath)
        {
            if (ofField.FieldType == fieldType.scalar)
            {
                ProbeBinaryIO.WriteScalars(binPath, ResultScalar);
            }
            else if (ofField.FieldType == fieldType.vector)
            {
                ProbeBinaryIO.WriteVectors(binPath, ResultVec);
            }
        }

        private void LoadFromBinaryCache(OFField ofField, string binPath)
        {
            if (ofField.FieldType == fieldType.scalar)
            {
                ResultScalar = ProbeBinaryIO.LoadScalars(binPath);
            }
            else if (ofField.FieldType == fieldType.vector)
            {
                ResultVec = ProbeBinaryIO.LoadVectors(binPath);
            }
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Gets the path to probed results for the latest time step.
        /// </summary>
        public static string GetPathToProbedResults(string workingDirectory, OFField ofField, OFResult result)
        {
            int lastIter = ProbeTimeHelper.GetLatestIteration(workingDirectory, null);
            string fullPath = ProbePathHelper.BuildProbeFilePath(workingDirectory, ofField.ProbeName, ofField.FieldName, lastIter);

            return File.Exists(fullPath) ? fullPath : string.Empty;
        }

        /// <summary>
        /// Gets the latest time step iteration number.
        /// </summary>
        public static int GetLatestTime(string workingDirectory, OFResult result, OFField ofField)
        {
            return ProbeTimeHelper.GetLatestIteration(workingDirectory, null);
        }

        /// <summary>
        /// Returns indices of probe points that are outside the simulation domain.
        /// </summary>
        public static int[] GetIndicesOutsideDomain(List<GH_Vector> vectors)
        {
            return vectors
                .Select((v, i) => new { Index = i, Length = v.Value.Length })
                .Where(x => x.Length > InvalidValueThreshold)
                .Select(x => x.Index)
                .ToArray();
        }

        /// <summary>
        /// Filters extreme probing values by replacing them with zero.
        /// </summary>
        public static double[] FilterExtremeValues(double[] values)
        {
            return values.Select(v => 
                (v < -InvalidValueThreshold || v > InvalidValueThreshold) ? 0.0 : v
            ).ToArray();
        }

        /// <summary>
        /// Filters out vectors with extreme lengths.
        /// </summary>
        public static Vector3d[] FilterExtremeVectors(Vector3d[] vectors)
        {
            return vectors.Where(v => v.Length < ExtremeVectorThreshold).ToArray();
        }

        /// <summary>
        /// Converts interpolation scheme index to OpenFOAM name.
        /// </summary>
        public static string ReformatIS(int schemeIndex)
        {
            return schemeIndex switch
            {
                1 => "cell",
                2 => "cellPoint",
                3 => "cellPointFace",
                4 => "pointMVC",
                5 => "cellPatchConstrained",
                _ => "cell"
            };
        }

        #endregion

        #region Backward Compatibility

        /// <summary>Legacy method - use GetIndicesOutsideDomain instead.</summary>
        public static int[] ReturnProbeIndicesOutsideDomain(List<GH_Vector> x) => GetIndicesOutsideDomain(x);

        /// <summary>Legacy method - use FilterExtremeValues instead.</summary>
        public static double[] FilterExtremeProbingValues(double[] inputList) => FilterExtremeValues(inputList);

        /// <summary>Legacy method - use FilterExtremeVectors instead.</summary>
        public static Vector3d[] FilterExtremeVectorLengths(Vector3d[] inputList) => FilterExtremeVectors(inputList);

        #endregion
    }
}
