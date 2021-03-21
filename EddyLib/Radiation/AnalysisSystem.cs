using System;
using System.Collections.Generic;
using System.Linq;
using DateTimeExtensions;

public class AnalysisBins
{
    public List<int> HoursInBin = new List<int>();

    public AnalysisBins(DateTime dt2, DateTime dt1)

    {
        if (dt1.Year == dt2.Year)
        {
            AddHours(dt1.HOY(), dt2.HOY());
        }
        else
        {
            AddHours(dt1.HOY(), 8760);
            AddHours(1, dt2.HOY());
        }
    }

    public AnalysisBins(DateTime dt2, DateTime dt1, DayTime.DayTimeE DTE)
    {
        if (dt1.Year == dt2.Year)
        {
            AddHours(dt1.HOY(), dt2.HOY(), DTE);
        }
        else
        {
            AddHours(dt1.HOY(), 8760, DTE);
            AddHours(1, dt2.HOY(), DTE);
        }
    }

    public AnalysisBins(AnalysisBins AB1, AnalysisBins AB2)
    {
        IEnumerable<int> both = AB1.HoursInBin.Intersect(AB2.HoursInBin);
        this.HoursInBin = both.ToList();
    }

    private void AddHours(int HOYstart, int HOYend)
    {
        for (int s = HOYstart; s < HOYend; s++)
        {
            HoursInBin.Add(s);
        }
    }

    private void AddHours(int HOYstart, int HOYend, DayTime.DayTimeE DTE)
    {
        var DayTime = new DayTime(DTE);

        for (int s = HOYstart; s < HOYend; s++)
        {
            var currentHour = DateTimeExtension.FromHOY(s);
            var currentDayTime = DayTime.getDayTime(currentHour);

            if (currentDayTime == DTE)
                HoursInBin.Add(s);
        }
    }

    // Downselect
}

public class Season
{
    public enum SeasonE
    {
        Winter, // 12,1,2
        Spring, // 3,4,5
        Summer, // 6,7,8
        Fall //  9,10,11
    }

    public int YearBegin { get; set; }
    public int YearEnd { get; set; }

    public int MonthBegin { get; set; }
    public int MonthEnd { get; set; }

    public SeasonE season { get; set; }

    public Season(SeasonE S)
    {
        switch (S)
        {
            case SeasonE.Spring:
                this.MonthBegin = 3;
                this.MonthEnd = 5;
                this.YearBegin = 2021;
                this.YearEnd = 2021;

                break;

            case SeasonE.Summer:
                this.MonthBegin = 6;
                this.MonthEnd = 8;
                this.YearBegin = 2021;
                this.YearEnd = 2021;
                break;

            case SeasonE.Fall:
                this.MonthBegin = 9;
                this.MonthEnd = 11;
                this.YearBegin = 2021;
                this.YearEnd = 2021;
                break;

            case SeasonE.Winter:
                this.MonthBegin = 12;
                this.MonthEnd = 2;
                this.YearBegin = 2021;
                this.YearEnd = 2022;
                break;
        }
    }

    private Season.SeasonE getSeasonExact(DateTime date)
    {
        float value = (float)date.Month + date.Day / 100f;  // <month>.<day(2 digit)>
        if (value < 3.21 || value >= 12.22) return Season.SeasonE.Winter;   // Winter
        if (value < 6.21) return Season.SeasonE.Spring; // Spring
        if (value < 9.23) return Season.SeasonE.Summer; // Summer

        return Season.SeasonE.Fall;   // Autumn
    }

    //private int getSeason(DateTime date, bool ofSouthernHemisphere) {
    //    int hemisphereConst = ofSouthernHemisphere ? 2 : 0;
    //    Func<int, int> getReturn = (northern) => {
    //        return (northern + hemisphereConst) % 4;
    //    };
    //    float value = (float)date.Month + date.Day / 100f;  // <month>.<day(2 digit)>
    //    if (value < 3.21 || value >= 12.22) return getReturn(3);    // 3: Winter
    //    if (value < 6.21) return getReturn(0);  // 0: Spring
    //    if (value < 9.23) return getReturn(1);  // 1: Summer
    //    return getReturn(2);     // 2: Autumn
    //}
}

public class DayTime
{
    public enum DayTimeE
    {
        Morning,    // 12pm - 6am
        Breakfast,  // 7am - 11am
        Lunch,      // 12am - 3pm
        Afternoon,  // 4pm - 5pm
        Dinner,     // 6pm - 9pm
        Nightlife   // 10pm - 12pm
    }

    public int HourBegin { get; set; }
    public int HourEnd { get; set; }

    public DayTimeE dayTimeE { get; set; }

    public DayTime(DayTimeE D)
    {
        this.dayTimeE = D;

        switch (D)
        {
            case DayTimeE.Morning:
                this.HourBegin = 0;
                this.HourEnd = 6;
                break;

            case DayTimeE.Breakfast:

                this.HourBegin = 7;
                this.HourEnd = 11;
                break;

            case DayTimeE.Lunch:

                this.HourBegin = 12;
                this.HourEnd = 15;
                break;

            case DayTimeE.Afternoon:

                this.HourBegin = 16;
                this.HourEnd = 17;
                break;

            case DayTimeE.Dinner:

                this.HourBegin = 18;
                this.HourEnd = 21;
                break;

            case DayTimeE.Nightlife:

                this.HourBegin = 22;
                this.HourEnd = 23;
                break;
        }
    }

    public DayTime.DayTimeE getDayTime(DateTime dt)
    {
        DayTime.DayTimeE daytime = 0;

        if (dt.Hour <= 6 && dt.Hour >= 0)
        {
            daytime = DayTime.DayTimeE.Morning;
        }
        if (dt.Hour <= 11 && dt.Hour > 6)
        {
            daytime = DayTime.DayTimeE.Breakfast;
        }
        if (dt.Hour <= 15 && dt.Hour > 11)
        {
            daytime = DayTime.DayTimeE.Lunch;
        }
        if (dt.Hour <= 17 && dt.Hour > 15)
        {
            daytime = DayTime.DayTimeE.Afternoon;
        }
        if (dt.Hour <= 21 && dt.Hour > 17)
        {
            daytime = DayTime.DayTimeE.Dinner;
        }
        if (dt.Hour <= 24 && dt.Hour > 21)
        {
            daytime = DayTime.DayTimeE.Nightlife;
        }

        return daytime;
    }
}

public class AnalysisSystem
{
    public int[][] AnalysisHourBins { get; set; }

    public AnalysisSystem(List<AnalysisBins> ABs)
    {
        this.AnalysisHourBins = new int[ABs.Count][];

        for (int i = 0; i < ABs.Count; i++)
        {
            var hoursInBin = (int)ABs[i].HoursInBin.Count;

            AnalysisHourBins[i] = new int[hoursInBin];

            for (int j = 0; j < hoursInBin; j++)
            {
                AnalysisHourBins[i][j] = ABs[i].HoursInBin[j];
            }
        }
    }
}

namespace DateTimeExtensions
{
    // Extension methods must be defined in a static class.
    public static class DateTimeExtension
    {
        // This is the extension method.
        // The first parameter takes the "this" modifier
        // and specifies the type for which the method is defined.

        public static DateTime FromHOY(int HOY)
        {
            // We don't do leap years --> 2021

            DateTime BeginYear = new DateTime(2021, 1, 1, 0, 0, 0);

            var ts = new TimeSpan(HOY, 0, 0);

            return BeginYear + ts;
        }

        public static int HOY(this DateTime dt)
        {
            // We don't do leap years --> 2021

            var dt_new = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second);

            DateTime BeginYear = new DateTime(dt.Year, 1, 1, 0, 0, 0);

            var ts = dt_new - BeginYear;
            var HOY = ts.TotalHours;

            return (int)HOY;
        }
    }
}