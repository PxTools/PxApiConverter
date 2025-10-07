namespace PxApiConverter.Logging;

public class FileLoggerOptions
{
    // Relative or absolute path to log file. If relative, it is rooted at the content root.
    public string Path { get; set; } = "Logs/app.log";

    // Minimum log level written to file (optional override of global). If null global filtering applies.
    public string? MinLevel { get; set; }

    // If > 0 and file exceeds this size (in bytes) it will be archived with a timestamp suffix and a new file started.
    public long MaxFileSizeBytes { get; set; } = 0; // 0 = unlimited
}
