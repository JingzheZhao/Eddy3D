using System.Collections.Generic;
using System.IO;

namespace EddyLib
{
    internal static class ProbeTimeHelper
    {
        internal static int GetLatestIteration(string workingDirectory, int? writeInterval)
        {
            var iterationNumbers = new List<int>();

            foreach (var dir in Directory.GetDirectories(workingDirectory))
            {
                var name = Path.GetFileName(dir);
                if (int.TryParse(name, out int number))
                {
                    iterationNumbers.Add(number);
                }
            }

            if (iterationNumbers.Count == 0)
            {
                return 0;
            }

            iterationNumbers.Sort();
            int latest = iterationNumbers[iterationNumbers.Count - 1];

            if (writeInterval.HasValue && writeInterval.Value > 0 && latest % writeInterval.Value != 0)
            {
                if (iterationNumbers.Count > 1)
                {
                    latest = iterationNumbers[iterationNumbers.Count - 2];
                }
            }

            return latest;
        }
    }
}
