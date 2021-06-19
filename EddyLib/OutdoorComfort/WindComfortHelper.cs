namespace EddyLib.OutdoorComfort
{
    // This is a post-processing class
    public class WindComfortHelper
    {
        public enum PCMetric
        {
            LawsonGeneral,

            LawsonLDDC,

            Lawson2001,

            Davenport,

            NEN8100Comfort,

            NEN8100Safety,
        };

        public enum CompOperator
        {
            G, // Greater

            GOE,// GreaterOrEqual

            S // Smaller
        }

        public struct UThresholdInfo
        {
            public int Cat; // integer catefory

            public double UThres; // Velocity

            public double TimeThres; // in decimals

            public string Class; // Words

            public string ClassLetter; // Letter: e.g. A

            public CompOperator Operator; // ComparisonOperator
        }
    }
}