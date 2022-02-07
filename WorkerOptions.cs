namespace AnalyticsParser
{/// <summary>
/// This is a class for controlling application settings with "appsettings.json" file.
/// </summary>
    public class WorkerOptions
    {
        public string TargetPath { get; set; }
        public string SourcePath { get; set; }
        public string FileFilter { get; set; }
        public string ToEmail { get; set; }

    }
}