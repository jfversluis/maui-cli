using System.Diagnostics;
using System.Xml.Linq;
using Spectre.Console;

namespace Maui.Cli.Services;

internal sealed class NuGetService : INuGetService
{
    private readonly IAnsiConsole _console;

    public NuGetService(IAnsiConsole console)
    {
        _console = console;
    }

    public Task<int> AddLocalNuGetSourceAsync(DirectoryInfo packageDirectory, CancellationToken cancellationToken = default)
    {
        // For now, we'll just create/update a NuGet.config file in the solution directory
        return Task.FromResult(0);
    }

    public async Task<int> UpdatePackageReferencesAsync(FileInfo projectFile, string packageSource, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!projectFile.Exists)
            {
                _console.MarkupLine($"[red]Project file not found: {projectFile.FullName}[/]");
                return ExitCodeConstants.ProjectNotFound;
            }

            // Load the project file
            var doc = XDocument.Load(projectFile.FullName);
            var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

            // Find all PackageReference elements with Microsoft.Maui packages
            var packageReferences = doc.Descendants(ns + "PackageReference")
                .Where(pr => pr.Attribute("Include")?.Value.StartsWith("Microsoft.Maui", StringComparison.OrdinalIgnoreCase) == true)
                .ToList();

            if (packageReferences.Count == 0)
            {
                _console.MarkupLine("[yellow]No Microsoft.Maui package references found in project[/]");
                return ExitCodeConstants.Success;
            }

            // Get available packages from the source directory
            var packages = await GetMauiPackagesAsync(new DirectoryInfo(packageSource), cancellationToken);
            var packageVersions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var pkg in packages)
            {
                var parts = Path.GetFileNameWithoutExtension(pkg).Split('.');
                if (parts.Length < 2) continue;
                
                // Extract package name and version
                // Format: Microsoft.Maui.Core.9.0.0-preview.1.1234.nupkg
                var lastDot = pkg.LastIndexOf('.');
                var secondLastDot = pkg.LastIndexOf('.', lastDot - 1);
                var packageName = Path.GetFileNameWithoutExtension(pkg[..secondLastDot]);
                var version = Path.GetFileNameWithoutExtension(pkg[(secondLastDot + 1)..]);
                
                packageVersions[packageName] = version;
            }

            // Update package versions
            bool updated = false;
            foreach (var packageRef in packageReferences)
            {
                var packageName = packageRef.Attribute("Include")?.Value;
                if (packageName != null && packageVersions.TryGetValue(packageName, out var newVersion))
                {
                    var versionAttr = packageRef.Attribute("Version");
                    if (versionAttr != null)
                    {
                        var oldVersion = versionAttr.Value;
                        versionAttr.Value = newVersion;
                        _console.MarkupLine($"[green]Updated {packageName} from {oldVersion} to {newVersion}[/]");
                        updated = true;
                    }
                }
            }

            if (updated)
            {
                doc.Save(projectFile.FullName);
                _console.MarkupLine($"[green]Project file updated: {projectFile.Name}[/]");
            }

            return ExitCodeConstants.Success;
        }
        catch (Exception ex)
        {
            _console.MarkupLine($"[red]Error updating project file: {ex.Message}[/]");
            return ExitCodeConstants.ApplyFailed;
        }
    }

    public Task<IReadOnlyList<string>> GetMauiPackagesAsync(DirectoryInfo packageDirectory, CancellationToken cancellationToken = default)
    {
        if (!packageDirectory.Exists)
        {
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        }

        var packages = packageDirectory
            .GetFiles("*.nupkg", SearchOption.AllDirectories)
            .Where(f => f.Name.StartsWith("Microsoft.Maui", StringComparison.OrdinalIgnoreCase))
            .Select(f => f.FullName)
            .ToList();

        return Task.FromResult<IReadOnlyList<string>>(packages);
    }

    private async Task<int> RunDotNetCommandAsync(string arguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            return ExitCodeConstants.GeneralError;
        }

        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode;
    }
}
