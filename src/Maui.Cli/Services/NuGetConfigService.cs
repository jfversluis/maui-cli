using System.Xml;

namespace Maui.Cli.Services;

internal sealed class NuGetConfigService : INuGetConfigService
{
    private readonly IProcessRunner _processRunner;

    public NuGetConfigService(IProcessRunner processRunner)
    {
        _processRunner = processRunner;
    }

    public async Task AddOrUpdateSourceAsync(string directory, string sourceName, string sourceUrl, CancellationToken cancellationToken = default)
    {
        var nugetConfigPath = Path.Combine(directory, "NuGet.config");

        // Check if source already exists
        if (await SourceExistsAsync(directory, sourceName, cancellationToken))
        {
            // Update existing source
            await _processRunner.RunProcessAsync(
                "dotnet",
                $"nuget update source {sourceName} --source \"{sourceUrl}\" --configfile \"{nugetConfigPath}\"",
                directory,
                cancellationToken
            );
        }
        else
        {
            // Add new source
            await _processRunner.RunProcessAsync(
                "dotnet",
                $"nuget add source \"{sourceUrl}\" --name {sourceName} --configfile \"{nugetConfigPath}\"",
                directory,
                cancellationToken
            );
        }

        // Add package source mapping for Microsoft.Maui* packages
        await AddPackageSourceMappingAsync(nugetConfigPath, sourceName, "Microsoft.Maui*");
    }

    public async Task<bool> SourceExistsAsync(string directory, string sourceName, CancellationToken cancellationToken = default)
    {
        var result = await _processRunner.RunProcessAsync(
            "dotnet",
            "nuget list source --format short",
            directory,
            cancellationToken
        );

        if (result.ExitCode != 0 || string.IsNullOrEmpty(result.Output))
        {
            return false;
        }

        // Parse output to check if source exists
        var lines = result.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        return lines.Any(line => line.StartsWith($"  {sourceName} ", StringComparison.OrdinalIgnoreCase) ||
                                 line.StartsWith($"{sourceName} ", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task AddPackageSourceMappingAsync(string nugetConfigPath, string sourceName, string packagePattern)
    {
        if (!File.Exists(nugetConfigPath))
        {
            return;
        }

        try
        {
            var doc = new XmlDocument { PreserveWhitespace = true };
            doc.Load(nugetConfigPath);

            var root = doc.DocumentElement;
            if (root == null)
            {
                return;
            }

            // Find or create packageSourceMapping element
            var packageSourceMapping = root.SelectSingleNode("packageSourceMapping") as XmlElement;
            if (packageSourceMapping == null)
            {
                packageSourceMapping = doc.CreateElement("packageSourceMapping");
                root.AppendChild(packageSourceMapping);
            }

            // Find or create packageSource element for this source
            var packageSource = packageSourceMapping.SelectSingleNode($"packageSource[@key='{sourceName}']") as XmlElement;
            if (packageSource == null)
            {
                packageSource = doc.CreateElement("packageSource");
                packageSource.SetAttribute("key", sourceName);
                packageSourceMapping.AppendChild(packageSource);
            }

            // Check if pattern already exists
            var existingPattern = packageSource.SelectSingleNode($"package[@pattern='{packagePattern}']");
            if (existingPattern == null)
            {
                var package = doc.CreateElement("package");
                package.SetAttribute("pattern", packagePattern);
                packageSource.AppendChild(package);
            }

            doc.Save(nugetConfigPath);
        }
        catch
        {
            // Ignore errors - NuGet config will still work without package source mappings
        }
    }
}
