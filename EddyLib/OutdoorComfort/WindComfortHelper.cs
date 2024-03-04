namespace EddyLib.OutdoorComfort
{
    // This is a post-processing class
    public class WindComfortHelper
    {
        public enum PedCmftMetric
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

        public struct CmftThresholdInfo
        {
            public int Cat; // integer catefory

            public double UThres; // Velocity

            public double TimeThres; // in decimals

            public string Class; // Stringyfied Class

            public string ClassLetter; // Letter Class: e.g. A

            public CompOperator Operator; // ComparisonOperator
        }
    }
}