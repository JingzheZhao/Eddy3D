using EddyLib;
using System.Collections.Generic;
using Xunit;

namespace RhinoPlugin.Test.Xunit.Tests
{
    public class TimeTests
    {
        [Fact]
        public void GetEvalHoursFromLB_FullYear_Returns8760Hours()
        {
            List<string> LBanalysis = new List<string> { "(1, 1, 1)", "(12, 31, 24)" };
            var hours = Utilities.GetEvalHoursFromLB(LBanalysis);

            Assert.Equal(8760, hours.Count);
        }

        [Fact]
        public void GetEvalHoursFromLB_January_Returns744HoursEndingAt744()
        {
            List<string> LBanalysis = new List<string> { "(1, 1, 1)", "(1, 31, 24)" };
            var hours = Utilities.GetEvalHoursFromLB(LBanalysis);

            Assert.Equal(744, hours.Count);
            Assert.Equal(744, hours[hours.Count - 1]);
        }

        [Fact]
        public void GetEvalHoursFromLB_February_Returns672HoursStartingAt745()
        {
            List<string> LBanalysis = new List<string> { "(2, 1, 1)", "(2, 28, 24)" };
            var hours = Utilities.GetEvalHoursFromLB(LBanalysis);

            Assert.Equal(672, hours.Count);
            Assert.Equal(745, hours[0]);
        }
    }
}
