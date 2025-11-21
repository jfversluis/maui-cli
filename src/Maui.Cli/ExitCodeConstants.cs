namespace Maui.Cli;

internal static class ExitCodeConstants
{
    public const int Success = 0;
    public const int GeneralError = 1;
    public const int DownloadFailed = 2;
    public const int ApplyFailed = 3;
    public const int InvalidPR = 4;
    public const int ProjectNotFound = 5;
    public const int FailedToFindProject = 6;
    public const int Cancelled = 7;
}
