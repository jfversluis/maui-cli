namespace Maui.Cli.Services;

internal interface INuGetConfigService
{
    Task AddOrUpdateSourceAsync(string directory, string sourceName, string sourceUrl, CancellationToken cancellationToken = default);
    Task<bool> SourceExistsAsync(string directory, string sourceName, CancellationToken cancellationToken = default);
}
