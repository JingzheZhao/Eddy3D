using System;
using System.Collections.Generic;
using System.Linq;

namespace EddyLib
{
    public class NVAnalysis
    {
        public double FlowRate;

        public double vCenter;

        public double ACR;

        public NVAnalysis(List<double> listOfCps, List<double> AreaList, double velocity)
        {
            this.FlowRate = GetFlowRate(listOfCps, AreaList, velocity);
        }

        public NVAnalysis(List<double> listOfCps, List<double> AreaList, double velocity, double VolumeZone)
        {
            this.FlowRate = GetFlowRate(listOfCps, AreaList, velocity);
            this.vCenter = GetvCenterNode(VolumeZone, FlowRate, AreaList, listOfCps);
            this.ACR = GetACR(VolumeZone, FlowRate);
        }

        private static double GetFlowRate(List<double> listOfCps, List<double> AreaList, double velocity)
        {
            // Airflow assessment in cross-ventilated buildings with operable façade elements
            // P.KaravaaT.StathopoulosbA.K.Athienitisb https://www.sciencedirect.com/science/article/pii/S0360132310002271

            var C_D_general = 0.7;

            double sumCpPos = 0;
            int countCpPosVals = 0;
            double sumCpNeg = 0;
            int countCpNegVals = 0;

            foreach (var cp in listOfCps)
            {
                if (cp > 0)
                {
                    sumCpPos += cp;
                    countCpPosVals++;
                }
                else if (cp < 0)
                {
                    sumCpNeg += cp;
                    countCpNegVals++;
                }
            }

            double AverageCpPos = 0;
            if (countCpPosVals > 0)
            {
                AverageCpPos = sumCpPos / countCpPosVals;
            }

            double AverageCpNeg = 0;
            if (countCpNegVals > 0)
            {
                AverageCpNeg = sumCpNeg / countCpNegVals;
            }

            double sumAreaCpNeg = 0;
            int countCpNeg = 0;
            double sumAreaCpPos = 0;
            int countCpPos = 0;

            int limit = Math.Min(listOfCps.Count, AreaList.Count);
            for (int i = 0; i < limit; i++)
            {
                if (listOfCps[i] < 0)
                {
                    sumAreaCpNeg += AreaList[i];
                    countCpNeg++;
                }
                else if (listOfCps[i] > 0)
                {
                    sumAreaCpPos += AreaList[i];
                    countCpPos++;
                }
            }

            // returns the corresponding areas where the cps were negative
            if (countCpNeg == 0) throw new InvalidOperationException("Sequence contains no elements");
            double AverageAreaCpNeg = sumAreaCpNeg / countCpNeg;

            // returns the corresponding areas where the cps were positive
            if (countCpPos == 0) throw new InvalidOperationException("Sequence contains no elements");
            double AverageAreaCpPos = sumAreaCpPos / countCpPos;

            var AverageCDCPNeg = C_D_general;
            if (countCpNeg == 0) throw new InvalidOperationException("Sequence contains no elements");

            var AverageCDCPPos = C_D_general;
            if (countCpPos == 0) throw new InvalidOperationException("Sequence contains no elements");

            // Do we need to compute a weighted average first?

            var C_D_tot_A = ((AverageCDCPPos * AverageAreaCpPos * AverageCDCPNeg * AverageAreaCpNeg) / Math.Sqrt(Math.Pow(AverageCDCPNeg * AverageAreaCpNeg, 2) + Math.Pow(AverageCDCPPos * AverageAreaCpPos, 2)));

            double deltaCp = Math.Abs(AverageCpPos - AverageCpNeg);

            var VolumetricFlowRate = C_D_tot_A * velocity * Math.Sqrt(deltaCp);

            // This returns VolumetricFlowRate in m3/s
            return VolumetricFlowRate;
        }

        private static double GetACR(double Volume, double FlowRate)
        {
            double ACR = 0;

            ACR = FlowRate * 3600 / Volume;

            // This returns in 1/h
            return ACR;
        }

        private static double GetvCenterNode(double Volume, double FlowRate, List<double> AreaList, List<double> listOfCps)
        {
            double vCenterNode = 0;

            double sumAreaCpPos = 0;
            int countCpPos = 0;

            int limit = Math.Min(listOfCps.Count, AreaList.Count);
            for (int i = 0; i < limit; i++)
            {
                if (listOfCps[i] > 0)
                {
                    sumAreaCpPos += AreaList[i];
                    countCpPos++;
                }
            }

            if (countCpPos == 0) throw new InvalidOperationException("Sequence contains no elements");
            double AverageAreaCpPos = sumAreaCpPos / countCpPos;

            vCenterNode = FlowRate / AverageAreaCpPos;

            // This returns in m/s
            return vCenterNode;
        }
    }
}