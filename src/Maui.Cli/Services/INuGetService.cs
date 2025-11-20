namespace Maui.Cli.Services;

internal interface INuGetService
{
    Task<int> AddLocalNuGetSourceAsync(DirectoryInfo packageDirectory, CancellationToken cancellationToken = default);
    Task<int> UpdatePackageReferencesAsync(FileInfo projectFile, string packageSource, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetMauiPackagesAsync(DirectoryInfo packageDirectory, CancellationToken cancellationToken = default);
}
