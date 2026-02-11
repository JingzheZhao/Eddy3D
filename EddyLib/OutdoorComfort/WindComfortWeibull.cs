using System.Collections.Generic;
using System.Threading.Tasks;
using static EddyLib.OutdoorComfort.WindComfortHelper;

namespace EddyLib.OutdoorComfort
{
    // This is a post-processing class
    public class WindComfortWeibull
    {
        public double[] ValsPedWindCmftCat { get; set; }
        public string[] ValsPedWindCmftClassStringified { get; set; }
        public string[] ValsPedWindCmftClassLetter { get; set; }

        public CmftThresholdInfo[] ThresholdInfo { get; set; }

        public WindComfortWeibull(WindFactorsTemporal wft, PedCmftMetric cmftMetric)
        {
            CalcPedestrianComfort(wft, cmftMetric);
        }

        private void CalcPedestrianComfort(WindFactorsTemporal wft, PedCmftMetric cmftMetric)
        {
            int sensorCount = wft.ValuesTemporalAtProbingHeight.GetLength(1);

            this.ValsPedWindCmftCat = new double[sensorCount];
            this.ValsPedWindCmftClassStringified = new string[sensorCount];
            this.ValsPedWindCmftClassLetter = new string[sensorCount];
            this.ThresholdInfo = new CmftThresholdInfo[sensorCount];

            // WindFactorsAnnual
            // [ProbingPoints , 8760]

            // WindFactorsSpatial
            // [SimulatedWindDirs , 8760]

            Dictionary<int, CmftThresholdInfo> TID = WindComfortMetricsWeibull.ThresholdInfo(cmftMetric);

            Parallel.For(0, sensorCount, probe =>
            {
                // column is all hours from one wind direction
                double[] columnWFA = ExtractColumn(wft.ValuesTemporalAtProbingHeight, probe);

                // Move to 10m according to Blocken
                // column = column.Select(x => EddyLib.BCs.BoundaryCondition.ScaleABL(x, 1.75, ws.BCond.z0, 10)).ToArray();

                var threshold = WindComfortMetricsWeibull.CalcExceedance(columnWFA, TID);
                this.ThresholdInfo[probe] = threshold;
                this.ValsPedWindCmftCat[probe] = threshold.Cat;
                this.ValsPedWindCmftClassStringified[probe] = threshold.Class;
                this.ValsPedWindCmftClassLetter[probe] = threshold.ClassLetter;
            });
        }

        private static double[] ExtractColumn(double[,] matrix, int columnIndex)
        {
            int rowCount = matrix.GetLength(0);
            var column = new double[rowCount];
            for (int row = 0; row < rowCount; row++)
            {
                column[row] = matrix[row, columnIndex];
            }

            return column;
        }
    }
}
