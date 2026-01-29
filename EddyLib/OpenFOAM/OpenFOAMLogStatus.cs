using System;

namespace EddyLib.OpenFOAM
{
    /// <summary>
    /// Parsed status information from an OpenFOAM log file.
    /// </summary>
    public sealed class OpenFOAMLogStatus
    {
        public string LogPath { get; internal set; }
        public bool HasLog { get; internal set; }

        public bool IsFinished { get; internal set; }
        public bool HasError { get; internal set; }
        public string ErrorMessage { get; internal set; }

        public string Phase { get; internal set; }

        public double? CurrentIteration { get; internal set; }
        public double? TotalIterations { get; internal set; }

        public int? MorphIteration { get; internal set; }
        public int? MorphIterationsTotal { get; internal set; }

        public TimeSpan? EstimatedRemaining { get; internal set; }
        public string LastLogLine { get; internal set; }
    }
}
