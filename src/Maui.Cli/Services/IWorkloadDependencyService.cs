using Maui.Cli.Models;

namespace Maui.Cli.Services;

internal interface IWorkloadDependencyService
{
    Task<WorkloadDependencyInfo?> GetWorkloadDependenciesAsync(string workloadName, string? sdkVersion = null);
    Task<IReadOnlyList<WorkloadDependencyInfo>> GetAllWorkloadDependenciesAsync(string? sdkVersion = null);
}
