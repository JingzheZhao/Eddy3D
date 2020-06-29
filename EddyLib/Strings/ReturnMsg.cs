using System.Text;

namespace EddyLib.Strings
{
    public class ReturnMsg

    {
        public static string MeshDoesntExist(string caseFolder)
        {
            return @"The mesh for case  """ + caseFolder + @""" does not exist.";
        }

        public static string FieldDoesntExist(string currentCaseDir, OFField field)
        {
            return @"The file """ + currentCaseDir + @"\postProcessing\" + field.ProbeName + @"\" + Utilities.GetLastIterationFromDirectory(currentCaseDir).ToString() + @"\" + field.FieldName + @""" does not exist. Please run the probing component.";
        }

        public static string ParsingFailed()
        {
            return @"Parsing of the probes failed. This data does not exist yet. Please run the probing component.";
        }

        public static string SelectionOutsideWindDirs(int dir)
        {
            return @"The wind direction " + dir + @" is outside the bounds of simulated wind directions.";
        }

        public static string PleaseRunProbingComponent()
        {
            return @"Please run the probing component.";
        }

        public static string PointsOutsideDomain(int[] IndecesOfExtremeProbes)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(@"The probes with the indices:

");

            for (int i = 0; i < IndecesOfExtremeProbes.Length; i++)
            {
                sb.Append(IndecesOfExtremeProbes.ToString() + " ,");

                if (i == 10)
                {
                    sb.Append(@"\n");
                }
            }
            sb.Append(@"

can't be probed within the simulation domain and have been discarded.");

            return sb.ToString();
        }

        public static string ProbingFuncObjects(OFResult RES, OFField field)
        {
            return @"You are probing a function object (" + field.FieldName + ") and WriteInt is set to " + RES.RunSettings.iter + ". Please lower the WriteInt to 1 and simulate one more iteration to enable the probing for this special case.";
        }

        public static string WrongNumberOfProbes(OFResult RES, string Engine)
        {
            return @"The precalculated " + Engine + " results do not have the correct number of probing points. The results need to be recalculated";
        }

        public static string PrecalResLoaded(OFResult RES, string Engine)
        {
            return "The precalculated " + Engine + " results have been loaded.";
        }

        public static string NoResults(OFResult RES, string Engine)
        {
            return "Either precalculated " + Engine + " results could not be loaded or the MRT array has not been calculated yet.";
        }

        public static string LargeDataTree(double threshold)
        {
            return "There are more than " + threshold.ToString("0.0E0") + " items in the data tree. Please be careful when connecting them to another component for post-processing as this might slow things down significantly.";
        }
    }
}