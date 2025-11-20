namespace Maui.Cli.Models;

internal sealed class PRArtifactInfo
{
    public required int PullRequestNumber { get; init; }
    public required string BuildId { get; init; }
    public required string ArtifactName { get; init; }
    public required string DownloadUrl { get; init; }
    public required long SizeInBytes { get; init; }
}
