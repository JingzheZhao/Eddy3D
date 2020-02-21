using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EddyLib.Strings
{
    public class RTMsg

    {
        public static string MeshDoesntExist(string caseFolder)
        {
            return @"The mesh for case  """ + caseFolder + @""" does not exist.";
        }

        public static string FieldDoesntExist(string currentCaseDir, string probeName)
        {
            return @"The file """ + currentCaseDir + @"\postProcessing\" + probeName + @""" does not exist. Please run the probing component.";
        }

        public static string ParsingFailed()
        {
            return @"Parsing of the probes failed. This data does not exist yet. Please run the probing component.";
        }

        public static string PleaseRunProbingComponent()
        {
            return @"Please run the probing component.";
        }

        public static string PointsOutsideDomain(int[] IndecesOfExtremeProbes)
        {
            return @"The probes with the indices: " + string.Join(",", IndecesOfExtremeProbes) + " can't be probed within the simulation domain and have been discarded.";
        }
    }
}