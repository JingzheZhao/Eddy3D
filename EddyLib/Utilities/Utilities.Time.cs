using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace EddyLib
{
    public static partial class Utilities
    {
        public static string ConvertComputeTimes(long elapsedMilliseconds)
        {
            string elapsedTime = "";

            if (elapsedMilliseconds < 60 * 1000)
            {
                elapsedTime = ("Compute time: " + elapsedMilliseconds / 1000 + " s");
            }
            else
            {
                elapsedTime = ("Compute time: " + elapsedMilliseconds / 1000 + " s or ca. " + elapsedMilliseconds / 1000 / 60 + " min");
            }

            return elapsedTime;
        }

        public static List<int> GetFullHoursListFromLB(List<string> LBanalysisList)
        {
            List<int> fullHoursList = new List<int>();

            //foreach (string LBobj in LBanalysisList)
            //{
            foreach (int hour in GetEvalHoursFromLB(LBanalysisList))
            {
                fullHoursList.Add(hour);
            }

            //}
            return fullHoursList;
        }

        public static List<int> GetFullHoursListFromInt(List<Interval> list)
        {
            List<int> fullHoursList = new List<int>();
            foreach (Interval inter in list)
            {
                foreach (int hour in GetEvalHoursFromInterval(inter))
                {
                    fullHoursList.Add(hour);
                }
            }
            return fullHoursList;
        }

        private static List<int> GetEvalHoursFromInterval(Interval inter)
        {
            List<int> evalHours = new List<int>();

            int startHour = (int)inter.T0;
            int endHour = (int)inter.T1;

            int numberOfHours = endHour - startHour;

            for (int i = startHour; i < startHour + numberOfHours; i++)
            {
                evalHours.Add(i);
            }
            return evalHours;
        }

        public static List<int> GetEvalHoursFromLB(List<string> LBanalysis)
        {
            List<int> hoursToEvaluate = new List<int>();

            int month_start = int.Parse(LBanalysis[0].Split(',')[0].Split('(')[1]) - 1;
            int month_end = int.Parse(LBanalysis[1].Split(',')[0].Split('(')[1]);
            int day_start = int.Parse(LBanalysis[0].Split(',')[1]) - 1;
            int day_end = int.Parse(LBanalysis[1].Split(',')[1]);
            int hour_start = int.Parse(LBanalysis[0].Split(',')[2].Split(')')[0]) - 1;
            int hour_end = int.Parse(LBanalysis[1].Split(',')[2].Split(')')[0]);

            if (month_end > 12) { month_end = 12; }
            if (day_end > 31) { day_end = 31; }
            if (hour_end > 24) { hour_end = 24; }

            int cnt = 0;
            int[] daysInMonth = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

            for (int m = 0; m < 12; m++) // 0-11
            {
                for (int d = 0; d < daysInMonth[m]; d++)
                {
                    for (int h = 0; h < 24; h++) // 0-23
                    {
                        cnt++;

                        // Fill list
                        if (m >= month_start && m < month_end && d >= day_start && d < day_end && h >= hour_start && h < hour_end)
                        {
                            hoursToEvaluate.Add(cnt);
                        }
                    }
                }
            }

            return hoursToEvaluate;
        }
    }
}
