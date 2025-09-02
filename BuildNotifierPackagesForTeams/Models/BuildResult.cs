using System;

namespace BuildNotifierPackagesForTeams.Models
{
    public class BuildResult
    {
        public bool Success { get; set; }
        public TimeSpan BuildTime { get; set; }
        public DateTime Timestamp { get; set; }
        public string ProjectName { get; set; }
    }
}