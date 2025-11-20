namespace Maui.Cli;

internal sealed class CliExecutionContext
{
    public DirectoryInfo WorkingDirectory { get; }
    public DirectoryInfo HivesDirectory { get; }
    public DirectoryInfo CacheDirectory { get; }
    public bool DebugMode { get; }

    public CliExecutionContext(
        DirectoryInfo workingDirectory,
        DirectoryInfo hivesDirectory,
        DirectoryInfo cacheDirectory,
        bool debugMode)
    {
        WorkingDirectory = workingDirectory;
        HivesDirectory = hivesDirectory;
        CacheDirectory = cacheDirectory;
        DebugMode = debugMode;
    }
}
