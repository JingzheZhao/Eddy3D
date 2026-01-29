using System;

namespace EddyLib.OpenFOAM
{
    /// <summary>
    /// Formatting helpers for OpenFOAM status output.
    /// </summary>
    public static class OpenFOAMStatusFormatter
    {
        public static string FormatEta(OpenFOAMLogStatus status)
        {
            if (status == null)
                return "unknown";

            if (!status.HasLog)
                return "log not found";

            if (status.HasError)
                return "error";

            if (status.IsFinished)
                return "done";

            if (status.EstimatedRemaining == null)
                return "unknown";

            return FormatTimeSpan(status.EstimatedRemaining.Value);
        }

        public static string FormatTimeSpan(TimeSpan span)
        {
            if (span < TimeSpan.Zero)
                span = TimeSpan.Zero;

            var totalHours = (int)Math.Floor(span.TotalHours);
            return $"{totalHours:D2}:{span.Minutes:D2}:{span.Seconds:D2}";
        }
    }
}
