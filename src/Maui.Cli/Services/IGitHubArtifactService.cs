using Maui.Cli.Models;

namespace Maui.Cli.Services;

internal interface IGitHubArtifactService
{
    Task<IReadOnlyList<PRArtifactInfo>> GetPRArtifactsAsync(int prNumber, CancellationToken cancellationToken = default);
    Task<string> DownloadArtifactAsync(PRArtifactInfo artifact, DirectoryInfo targetDirectory, IProgress<double>? progress = null, CancellationToken cancellationToken = default);
}
