//using Rhino.Compute;

using System;
using System.Collections.Generic;
using Xunit;

namespace RhinoPlugin.Test.Xunit
{
    [Collection("Rhino Collection")]
    public class PostProcessingTests
    {
        [Fact]
        public void AnalysisBins_Annual()
        {
            // Arrange

            var Winter = new Season((Season.SeasonE.Winter));

            DateTime w1 = new DateTime(Winter.YearBegin, Winter.MonthBegin, Winter.DayBegin);
            DateTime w2 = new DateTime(Winter.YearEnd, Winter.MonthEnd, Winter.DayEnd);

            AnalysisBins Wi = new AnalysisBins(w2, w1);

            var Spring = new Season((Season.SeasonE.Spring));

            DateTime sp1 = new DateTime(Spring.YearBegin, Spring.MonthBegin, Spring.DayBegin);
            DateTime sp2 = new DateTime(Spring.YearEnd, Spring.MonthEnd, Spring.DayEnd);

            AnalysisBins Sp = new AnalysisBins(sp2, sp1);

            var Summer = new Season((Season.SeasonE.Summer));

            DateTime su1 = new DateTime(Summer.YearBegin, Summer.MonthBegin, Summer.DayBegin);
            DateTime su2 = new DateTime(Summer.YearEnd, Summer.MonthEnd, Summer.DayEnd);

            AnalysisBins Su = new AnalysisBins(su2, su1);

            var Fall = new Season((Season.SeasonE.Fall));

            DateTime fa1 = new DateTime(Fall.YearBegin, Fall.MonthBegin, Fall.DayBegin);
            DateTime fa2 = new DateTime(Fall.YearEnd, Fall.MonthEnd, Fall.DayEnd);

            AnalysisBins Fa = new AnalysisBins(fa2, fa1);

            var a = new AnalysisSystem(new List<AnalysisBins> { Wi, Sp, Su, Fa });
            var allHours = a.AnalysisHourBins[0].Length + a.AnalysisHourBins[1].Length + a.AnalysisHourBins[2].Length + a.AnalysisHourBins[3].Length;
            Assert.Equal(8760, allHours);
        }
    }
}