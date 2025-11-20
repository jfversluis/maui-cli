using Maui.Cli.Models;

namespace Maui.Cli.Services;

internal interface IEnvironmentCheckService
{
    Task<IReadOnlyList<CheckResult>> CheckEnvironmentAsync(string? platform, bool verbose, string? manifestUrl = null);
}
