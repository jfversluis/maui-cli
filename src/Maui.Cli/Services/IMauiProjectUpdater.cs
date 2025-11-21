namespace Maui.Cli.Services;

internal interface IMauiProjectUpdater
{
    Task<ProjectUpdateResult> UpdateProjectAsync(FileInfo projectFile, Models.MauiChannel channel, CancellationToken cancellationToken = default);
}

internal sealed class ProjectUpdateResult
{
    public bool UpdatesApplied { get; set; }
    public List<string> UpdatedPackages { get; set; } = new();
    public string? TargetFramework { get; set; }
    public bool TargetFrameworkUpdated { get; set; }
    public string? NewTargetFramework { get; set; }
}
