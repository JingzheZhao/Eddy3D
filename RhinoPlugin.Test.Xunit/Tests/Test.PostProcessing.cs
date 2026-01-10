//using Rhino.Compute;

using System;
using System.Collections.Generic;
using Xunit;

namespace RhinoPlugin.Test.Xunit
{
    [Collection("Rhino Collection")]
    public class PostProcessingTests
    {
        private static AnalysisBins CreateSeasonBins(Season.SeasonE seasonType)
        {
            var season = new Season(seasonType);
            var start = new DateTime(season.YearBegin, season.MonthBegin, season.DayBegin);
            var end = new DateTime(season.YearEnd, season.MonthEnd, season.DayEnd);
            return new AnalysisBins(end, start);
        }

        [Fact]
        public void AnalysisBins_Annual()
        {
            // Arrange
            var bins = new List<AnalysisBins>
            {
                CreateSeasonBins(Season.SeasonE.Winter),
                CreateSeasonBins(Season.SeasonE.Spring),
                CreateSeasonBins(Season.SeasonE.Summer),
                CreateSeasonBins(Season.SeasonE.Fall)
            };

            var a = new AnalysisSystem(bins);
            var allHours = 0;
            foreach (var hourBin in a.AnalysisHourBins)
            {
                allHours += hourBin.Length;
            }
            Assert.Equal(TestConstants.HoursPerYear, allHours);
        }
    }
}
