using System;
using System.Collections.Generic;

public class TimeDiff
{
    public TimeSpan TS { get; set; }
    public double HOY { get; set; }
    public DateTime BeginYear = new DateTime(2021, 1, 1, 1, 1, 1);

    public TimeDiff(DateTime dt2, DateTime dt1)
    {
        if (dt2 < dt1)
        {
            var ts = dt1 - BeginYear;
            this.HOY = ts.TotalHours;
            this.TS = (dt1 - BeginYear) - (dt2 - BeginYear);
        }
        else
        {
            var ts = dt2 - BeginYear;
            this.HOY = ts.TotalHours;
            this.TS = (dt2 - BeginYear) - (dt1 - BeginYear);
        }
    }
}

public class AnalysisSystem
{
    public int[][] AnalysisHourBins { get; set; }

    public AnalysisSystem(List<TimeDiff> TimeSpans)
    {
        this.AnalysisHourBins = new int[TimeSpans.Count][];

        for (int i = 0; i < TimeSpans.Count; i++)
        {
            var hoursInBin = (int)TimeSpans[i].TS.TotalHours;

            AnalysisHourBins[i] = new int[hoursInBin];

            for (int j = 0; j < hoursInBin; j++)
            {
                AnalysisHourBins[i][j] = (int)TimeSpans[i].HOY + j;
            }
        }
    }
}