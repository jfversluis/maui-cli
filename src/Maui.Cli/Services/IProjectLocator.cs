namespace Maui.Cli.Services;

internal interface IProjectLocator
{
    Task<FileInfo?> FindProjectFileAsync(string directory, CancellationToken cancellationToken = default);
}
