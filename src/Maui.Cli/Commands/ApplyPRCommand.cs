using System.CommandLine;
using Maui.Cli.Models;
using Maui.Cli.Services;
using Spectre.Console;

namespace Maui.Cli.Commands;

internal sealed class ApplyPRCommand : Command
{
    private readonly IGitHubArtifactService _githubService;
    private readonly INuGetService _nugetService;
    private readonly IAnsiConsole _console;
    private readonly CliExecutionContext _context;

    public ApplyPRCommand(
        IGitHubArtifactService githubService,
        INuGetService nugetService,
        IAnsiConsole console,
        CliExecutionContext context)
        : base("apply-pr", "Download artifacts from a .NET MAUI PR and apply them to your project")
    {
        _githubService = githubService;
        _nugetService = nugetService;
        _console = console;
        _context = context;

        var prNumberArg = new Argument<int>("pr-number", "The pull request number from dotnet/maui repository");
        AddArgument(prNumberArg);

        var projectOption = new Option<FileInfo?>(
            "--project",
            "Path to the .csproj file to update. If not specified, searches current directory");
        projectOption.AddAlias("-p");
        AddOption(projectOption);

        this.SetHandler(ExecuteAsync, prNumberArg, projectOption);
    }

    private async Task<int> ExecuteAsync(int prNumber, FileInfo? projectFile)
    {
        try
        {
            _console.MarkupLine($"[blue]Fetching artifacts for PR #{prNumber}...[/]");

            // Get artifacts for the PR
            var artifacts = await _githubService.GetPRArtifactsAsync(prNumber);
            
            if (artifacts.Count == 0)
            {
                _console.MarkupLine("[red]No artifacts found for this PR. The PR may not have completed builds yet.[/]");
                return ExitCodeConstants.InvalidPR;
            }

            // Display available artifacts
            _console.MarkupLine($"[green]Found {artifacts.Count} artifact(s)[/]");
            
            PRArtifactInfo? selectedArtifact;
            
            if (artifacts.Count == 1)
            {
                selectedArtifact = artifacts[0];
                _console.MarkupLine($"[blue]Using artifact: {selectedArtifact.ArtifactName}[/]");
            }
            else
            {
                // Let user select which artifact to download
                selectedArtifact = _console.Prompt(
                    new SelectionPrompt<PRArtifactInfo>()
                        .Title("Select artifact to download:")
                        .AddChoices(artifacts)
                        .UseConverter(a => $"{a.ArtifactName} ({FormatBytes(a.SizeInBytes)})")
                );
            }

            // Create hive directory for this PR
            var prHiveDir = new DirectoryInfo(Path.Combine(_context.HivesDirectory.FullName, $"pr-{prNumber}"));
            
            // Download the artifact
            var extractedPath = await _console.Status()
                .StartAsync($"Downloading {selectedArtifact.ArtifactName}...", async ctx =>
                {
                    var progress = new Progress<double>(p =>
                    {
                        ctx.Status($"Downloading {selectedArtifact.ArtifactName}... {p:F1}%");
                    });
                    
                    return await _githubService.DownloadArtifactAsync(selectedArtifact, prHiveDir, progress);
                });

            _console.MarkupLine($"[green]✓ Downloaded to: {extractedPath}[/]");

            // Find the project file if not specified
            if (projectFile == null)
            {
                var csprojFiles = _context.WorkingDirectory.GetFiles("*.csproj", SearchOption.TopDirectoryOnly);
                
                if (csprojFiles.Length == 0)
                {
                    _console.MarkupLine("[red]No .csproj file found in current directory. Use --project to specify the path.[/]");
                    return ExitCodeConstants.ProjectNotFound;
                }
                
                if (csprojFiles.Length == 1)
                {
                    projectFile = csprojFiles[0];
                    _console.MarkupLine($"[blue]Using project: {projectFile.Name}[/]");
                }
                else
                {
                    projectFile = _console.Prompt(
                        new SelectionPrompt<FileInfo>()
                            .Title("Select project to update:")
                            .AddChoices(csprojFiles)
                            .UseConverter(f => f.Name)
                    );
                }
            }

            // Create NuGet.config to add the local source
            await CreateNuGetConfigAsync(prHiveDir);

            // Update the project to use packages from the downloaded artifacts
            _console.MarkupLine("[blue]Updating project package references...[/]");
            var updateResult = await _nugetService.UpdatePackageReferencesAsync(projectFile, extractedPath);
            
            if (updateResult == ExitCodeConstants.Success)
            {
                _console.MarkupLine("[green]✓ Project updated successfully![/]");
                _console.MarkupLine($"[yellow]Remember to restore packages: dotnet restore[/]");
            }

            return updateResult;
        }
        catch (Exception ex)
        {
            _console.MarkupLine($"[red]Error: {ex.Message}[/]");
            if (_context.DebugMode)
            {
                _console.WriteException(ex);
            }
            return ExitCodeConstants.GeneralError;
        }
    }

    private async Task CreateNuGetConfigAsync(DirectoryInfo prHiveDir)
    {
        var nugetConfigPath = Path.Combine(_context.WorkingDirectory.FullName, "NuGet.config");
        var nugetConfigExists = File.Exists(nugetConfigPath);

        var content = nugetConfigExists 
            ? await File.ReadAllTextAsync(nugetConfigPath)
            : @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
  </packageSources>
</configuration>";

        // Simple XML manipulation to add the source
        if (!content.Contains($"maui-pr-{Path.GetFileName(prHiveDir.FullName)}"))
        {
            var insertPoint = content.IndexOf("</packageSources>");
            if (insertPoint > 0)
            {
                var sourceEntry = $@"
    <add key=""maui-pr-{Path.GetFileName(prHiveDir.FullName)}"" value=""{prHiveDir.FullName}"" />";
                content = content.Insert(insertPoint, sourceEntry);
                await File.WriteAllTextAsync(nugetConfigPath, content);
                _console.MarkupLine($"[green]✓ Added local package source to NuGet.config[/]");
            }
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
