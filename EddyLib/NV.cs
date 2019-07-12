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

            List<double> cdList = new List<double>();

            for (int i = 0; i < listOfCps.Count; i++)
            {
                cdList.Add(C_D_general);
            }

            var listOfInputCpsPos = listOfCps.Where(x => x > 0).ToList();
            var listOfInputCpsNeg = listOfCps.Where(x => x < 0).ToList();

            double AverageCpPos = 0;
            if (listOfInputCpsPos.Count > 0)
            {
                AverageCpPos = listOfInputCpsPos.Average();
            }

            double AverageCpNeg = 0;
            if (listOfInputCpsNeg.Count > 0)
            {
                AverageCpNeg = listOfInputCpsNeg.Average();
            }

            // returns the corresponding areas where the cps were negative
            double AverageAreaCpNeg = AreaList.Where((x, index) => listOfCps.Select(cp => cp < 0).ToArray()[index]).ToList().Average();
            // returns the corresponding areas where the cps were positive
            double AverageAreaCpPos = AreaList.Where((x, index) => listOfCps.Select(cp => cp > 0).ToArray()[index]).ToList().Average();

            var AverageCDCPNeg = cdList.Where((x, index) => listOfCps.Select(cp => cp < 0).ToArray()[index]).Average();
            var AverageCDCPPos = cdList.Where((x, index) => listOfCps.Select(cp => cp > 0).ToArray()[index]).Average();

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
            double AverageAreaCpPos = AreaList.Where((x, index) => listOfCps.Select(cp => cp > 0).ToArray()[index]).ToList().Average();

            vCenterNode = FlowRate / AverageAreaCpPos;

            // This returns in m/s
            return vCenterNode;
        }
    }
}