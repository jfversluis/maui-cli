using Maui.Cli.Models;

namespace Maui.Cli.Services;

internal interface IManifestService
{
    Task<CheckManifest?> LoadManifestAsync(string? manifestUrl = null);
    CheckManifest GetDefaultManifest();
}
