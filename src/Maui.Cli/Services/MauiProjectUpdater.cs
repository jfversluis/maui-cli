using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using Maui.Cli.Models;
using Spectre.Console;

namespace Maui.Cli.Services;

internal sealed partial class MauiProjectUpdater : IMauiProjectUpdater
{
    private readonly IProcessRunner _processRunner;
    private readonly INuGetConfigService _nugetConfigService;

    public MauiProjectUpdater(IProcessRunner processRunner, INuGetConfigService nugetConfigService)
    {
        _processRunner = processRunner;
        _nugetConfigService = nugetConfigService;
    }

    public async Task<ProjectUpdateResult> UpdateProjectAsync(FileInfo projectFile, MauiChannel channel, CancellationToken cancellationToken = default)
    {
        var result = new ProjectUpdateResult();

        if (!projectFile.Exists)
        {
            throw new InvalidOperationException($"Project file not found: {projectFile.FullName}");
        }

        AnsiConsole.MarkupLine($"[cyan]Analyzing project:[/] {projectFile.Name}");
        AnsiConsole.WriteLine();

        var currentTargetFramework = await DetectTargetFrameworkAsync(projectFile);
        AnsiConsole.MarkupLine($"[dim]Current target framework:[/] {currentTargetFramework ?? "unknown"}");

        // Check if TFM upgrade is needed
        var needsTfmUpgrade = !string.IsNullOrEmpty(channel.TargetFramework) && 
                              !string.IsNullOrEmpty(currentTargetFramework) &&
                              !currentTargetFramework.Equals(channel.TargetFramework, StringComparison.OrdinalIgnoreCase);

        if (needsTfmUpgrade && currentTargetFramework != null)
        {
            AnsiConsole.MarkupLine($"[yellow]Note:[/] Selected channel targets {channel.TargetFramework}, but project uses {currentTargetFramework}");
            if (AnsiConsole.Confirm($"Do you want to upgrade the project to {channel.TargetFramework}?", true))
            {
                await UpdateTargetFrameworkAsync(projectFile, currentTargetFramework, channel.TargetFramework, cancellationToken);
                result.TargetFrameworkUpdated = true;
                result.NewTargetFramework = channel.TargetFramework;
                AnsiConsole.MarkupLine($"[green]✓[/] Updated target framework to {channel.TargetFramework}");
                AnsiConsole.WriteLine();
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]Skipping TFM upgrade. Package versions will be filtered for {currentTargetFramework}[/]");
                AnsiConsole.WriteLine();
            }
        }

        var mauiPackages = await GetMauiPackagesAsync(projectFile, cancellationToken);
        if (mauiPackages.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No MAUI packages found in project[/]");
            return result;
        }

        AnsiConsole.MarkupLine($"[dim]Found {mauiPackages.Count} MAUI package(s)[/]");
        AnsiConsole.WriteLine();

        // Get latest versions from the selected channel
        // Use the actual current TFM for version filtering if user declined TFM upgrade
        var tfmForFiltering = result.TargetFrameworkUpdated ? channel.TargetFramework : currentTargetFramework;
        
        var updates = new List<PackageUpdate>();
        foreach (var package in mauiPackages)
        {
            var latestVersion = await GetLatestPackageVersionAsync(package.Key, channel, tfmForFiltering, projectFile.DirectoryName!, cancellationToken);
            if (latestVersion != null && latestVersion != package.Value)
            {
                updates.Add(new PackageUpdate(package.Key, package.Value, latestVersion));
            }
        }

        if (updates.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]✓[/] All MAUI packages are up to date");
            return result;
        }

        // Display proposed updates
        var table = new Table();
        table.AddColumn("Package");
        table.AddColumn("Current Version");
        table.AddColumn("New Version");

        foreach (var update in updates)
        {
            table.AddRow(
                $"[bold]{update.PackageId}[/]",
                $"[yellow]{update.CurrentVersion}[/]",
                $"[green]{update.NewVersion}[/]"
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        if (!AnsiConsole.Confirm("Apply these updates?", true))
        {
            AnsiConsole.MarkupLine("[yellow]Update cancelled[/]");
            return result;
        }

        // Add/update NuGet feed if needed
        if (channel.Type == MauiChannelType.Nightly)
        {
            AnsiConsole.MarkupLine($"[dim]Configuring NuGet feed:[/] {channel.Name}");
            await _nugetConfigService.AddOrUpdateSourceAsync(
                projectFile.DirectoryName!,
                channel.Name,
                channel.FeedUrl,
                cancellationToken
            );
        }

        // Apply updates
        foreach (var update in updates)
        {
            AnsiConsole.MarkupLine($"[dim]Updating {update.PackageId}...[/]");
            await UpdatePackageReferenceAsync(projectFile, update, cancellationToken);
            result.UpdatedPackages.Add($"{update.PackageId} {update.CurrentVersion} → {update.NewVersion}");
        }

        result.UpdatesApplied = true;
        result.TargetFramework = currentTargetFramework;

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[green]✓[/] Update completed successfully");

        return result;
    }

    private async Task<string?> DetectTargetFrameworkAsync(FileInfo projectFile)
    {
        try
        {
            var doc = new XmlDocument();
            doc.Load(projectFile.FullName);

            // Check for TargetFramework first (single target)
            var targetFrameworkNode = doc.SelectSingleNode("//TargetFramework");
            if (targetFrameworkNode?.InnerText != null)
            {
                return ParseFrameworkVersion(targetFrameworkNode.InnerText);
            }

            // Check for TargetFrameworks (multi-targeting)
            var targetFrameworksNode = doc.SelectSingleNode("//TargetFrameworks");
            if (targetFrameworksNode?.InnerText != null)
            {
                var frameworks = targetFrameworksNode.InnerText.Split(';', StringSplitOptions.RemoveEmptyEntries);
                if (frameworks.Length > 0)
                {
                    return ParseFrameworkVersion(frameworks[0]);
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string? ParseFrameworkVersion(string targetFramework)
    {
        // Extract net9.0 from net9.0-android, net9.0-ios, etc.
        var match = TargetFrameworkRegex().Match(targetFramework);
        return match.Success ? match.Groups[1].Value : null;
    }

    private async Task<Dictionary<string, string>> GetMauiPackagesAsync(FileInfo projectFile, CancellationToken cancellationToken)
    {
        var packages = new Dictionary<string, string>();

        try
        {
            var doc = new XmlDocument();
            doc.Load(projectFile.FullName);

            var packageReferences = doc.SelectNodes("//PackageReference");
            if (packageReferences == null) return packages;

            foreach (XmlNode packageRef in packageReferences)
            {
                var includeAttr = packageRef.Attributes?["Include"];
                var versionAttr = packageRef.Attributes?["Version"];

                if (includeAttr?.Value != null && versionAttr?.Value != null)
                {
                    var packageId = includeAttr.Value;
                    if (IsMauiPackage(packageId))
                    {
                        packages[packageId] = versionAttr.Value;
                    }
                }
            }
        }
        catch
        {
            // Fallback: try to get packages using dotnet list package
            var result = await _processRunner.RunProcessAsync(
                "dotnet",
                $"list \"{projectFile.FullName}\" package --format json",
                projectFile.DirectoryName!,
                cancellationToken
            );

            if (result.ExitCode == 0 && !string.IsNullOrEmpty(result.Output))
            {
                try
                {
                    using var jsonDoc = JsonDocument.Parse(result.Output);
                    var projects = jsonDoc.RootElement.GetProperty("projects");
                    foreach (var project in projects.EnumerateArray())
                    {
                        if (project.TryGetProperty("frameworks", out var frameworks))
                        {
                            foreach (var framework in frameworks.EnumerateArray())
                            {
                                if (framework.TryGetProperty("topLevelPackages", out var topLevelPackages))
                                {
                                    foreach (var package in topLevelPackages.EnumerateArray())
                                    {
                                        var id = package.GetProperty("id").GetString();
                                        var resolvedVersion = package.GetProperty("resolvedVersion").GetString();

                                        if (id != null && resolvedVersion != null && IsMauiPackage(id))
                                        {
                                            packages[id] = resolvedVersion;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore JSON parsing errors
                }
            }
        }

        return packages;
    }

    private static bool IsMauiPackage(string packageId)
    {
        return packageId.StartsWith("Microsoft.Maui", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string?> GetLatestPackageVersionAsync(string packageId, MauiChannel channel, string? targetFramework, string workingDirectory, CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            
            // Get the service index to find the PackageBaseAddress
            var serviceIndexUrl = channel.FeedUrl;
            var serviceIndexResponse = await httpClient.GetStringAsync(serviceIndexUrl, cancellationToken);
            var serviceIndex = JsonDocument.Parse(serviceIndexResponse);
            
            // Find the PackageBaseAddress resource (flatcontainer)
            var resources = serviceIndex.RootElement.GetProperty("resources");
            string? packageBaseUrl = null;
            
            foreach (var resource in resources.EnumerateArray())
            {
                var type = resource.GetProperty("@type").GetString();
                if (type == "PackageBaseAddress/3.0.0")
                {
                    packageBaseUrl = resource.GetProperty("@id").GetString();
                    break;
                }
            }
            
            if (packageBaseUrl == null)
            {
                return null;
            }
            
            // Get all versions using the flatcontainer API
            var packageIdLower = packageId.ToLowerInvariant();
            var versionsUrl = $"{packageBaseUrl.TrimEnd('/')}/{packageIdLower}/index.json";
            var versionsResponse = await httpClient.GetStringAsync(versionsUrl, cancellationToken);
            var versionsDoc = JsonDocument.Parse(versionsResponse);
            
            // Parse versions array
            if (versionsDoc.RootElement.TryGetProperty("versions", out var versionsArray))
            {
                var versions = new List<string>();
                foreach (var version in versionsArray.EnumerateArray())
                {
                    var versionString = version.GetString();
                    if (versionString != null)
                    {
                        versions.Add(versionString);
                    }
                }
                
                if (versions.Count == 0)
                {
                    return null;
                }
                
                // Filter versions based on channel type and target framework
                IEnumerable<string> filteredVersions;
                if (channel.Type == MauiChannelType.Stable)
                {
                    // For stable channel, filter out prereleases
                    filteredVersions = versions.Where(v => !v.Contains("-"));
                    
                    // Further filter by TFM compatibility (net9 vs net10)
                    if (!string.IsNullOrEmpty(targetFramework))
                    {
                        if (targetFramework.StartsWith("net9"))
                        {
                            // For net9, exclude versions >= 10.0.0 (these are net10+ only)
                            filteredVersions = filteredVersions.Where(v =>
                            {
                                if (Semver.SemVersion.TryParse(v, Semver.SemVersionStyles.Any, out var semVer))
                                {
                                    return semVer.Major < 10;
                                }
                                return true;
                            });
                        }
                        else if (targetFramework.StartsWith("net10"))
                        {
                            // For net10, prefer versions >= 10.0.0
                            // But if none exist, fall back to latest
                            var net10Versions = filteredVersions.Where(v =>
                            {
                                if (Semver.SemVersion.TryParse(v, Semver.SemVersionStyles.Any, out var semVer))
                                {
                                    return semVer.Major >= 10;
                                }
                                return false;
                            }).ToList();
                            
                            if (net10Versions.Any())
                            {
                                filteredVersions = net10Versions;
                            }
                        }
                    }
                }
                else
                {
                    // For nightly channels, include prereleases
                    filteredVersions = versions;
                }
                
                // Sort using semantic versioning and return the highest version
                var semVersions = filteredVersions
                    .Select(v =>
                    {
                        try
                        {
                            return (Version: v, SemVer: Semver.SemVersion.Parse(v, Semver.SemVersionStyles.Any));
                        }
                        catch
                        {
                            return (Version: v, SemVer: (Semver.SemVersion?)null);
                        }
                    })
                    .Where(x => x.SemVer != null)
                    .OrderBy(x => x.SemVer)
                    .ToList();
                
                if (semVersions.Count == 0)
                {
                    return null;
                }
                
                return semVersions.Last().Version;
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }

    private async Task UpdatePackageReferenceAsync(FileInfo projectFile, PackageUpdate update, CancellationToken cancellationToken)
    {
        var content = await File.ReadAllTextAsync(projectFile.FullName, cancellationToken);
        
        // Try to update the Version attribute using regex to preserve formatting and comments
        var pattern = $@"(<PackageReference\s+Include=""{Regex.Escape(update.PackageId)}""\s+Version="")[^""]+("")";
        var replacement = $"$1{update.NewVersion}$2";
        
        if (Regex.IsMatch(content, pattern))
        {
            content = Regex.Replace(content, pattern, replacement);
            await File.WriteAllTextAsync(projectFile.FullName, content, cancellationToken);
        }
        else
        {
            // Fallback: use dotnet add package
            await _processRunner.RunProcessAsync(
                "dotnet",
                $"add \"{projectFile.FullName}\" package {update.PackageId} --version {update.NewVersion} --no-restore",
                projectFile.DirectoryName!,
                cancellationToken
            );
        }
    }

    private async Task UpdateTargetFrameworkAsync(FileInfo projectFile, string oldTfm, string newTfm, CancellationToken cancellationToken)
    {
        var content = await File.ReadAllTextAsync(projectFile.FullName, cancellationToken);
        
        // Update TFM references in the entire file (including XML content and comments)
        // Match net9.0 followed by word boundary or hyphen (for net9.0-android, net9.0-ios, etc.)
        content = Regex.Replace(content, $@"{Regex.Escape(oldTfm)}(?=\b|-)", newTfm);
        
        await File.WriteAllTextAsync(projectFile.FullName, content, cancellationToken);
    }

    [GeneratedRegex(@"(net\d+\.\d+)")]
    private static partial Regex TargetFrameworkRegex();

    private record PackageUpdate(string PackageId, string CurrentVersion, string NewVersion);
}
