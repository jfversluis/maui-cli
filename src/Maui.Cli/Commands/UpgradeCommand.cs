using System.CommandLine;
using Maui.Cli.Models;
using Maui.Cli.Services;
using Spectre.Console;

namespace Maui.Cli.Commands;

internal sealed class UpgradeCommand : Command
{
    private readonly IMauiProjectUpdater _projectUpdater;
    private readonly IProjectLocator _projectLocator;

    public UpgradeCommand(IMauiProjectUpdater projectUpdater, IProjectLocator projectLocator)
        : base("upgrade", "Upgrade .NET MAUI packages to the latest version")
    {
        _projectUpdater = projectUpdater;
        _projectLocator = projectLocator;

        var projectOption = new Option<FileInfo?>(
            "--project",
            "Path to the .csproj file to upgrade. If not specified, searches for a project in the current directory."
        );

        var channelOption = new Option<string?>(
            "--channel",
            "Channel to upgrade to: net9-stable, net10-stable, net9-nightly, or net10-nightly. If not specified, you will be prompted to choose."
        )
        {
            ArgumentHelpName = "channel"
        };

        AddOption(projectOption);
        AddOption(channelOption);

        this.SetHandler(ExecuteAsync, projectOption, channelOption);
    }

    private async Task<int> ExecuteAsync(FileInfo? projectFile, string? channelName)
    {
        try
        {
            AnsiConsole.Write(
                new FigletText("MAUI Upgrade")
                    .Color(Color.Purple)
            );
            AnsiConsole.WriteLine();

            // Locate project file
            if (projectFile == null)
            {
                projectFile = await _projectLocator.FindProjectFileAsync(Environment.CurrentDirectory);
                if (projectFile == null)
                {
                    AnsiConsole.MarkupLine("[red]Error:[/] No .csproj file found in the current directory.");
                    AnsiConsole.MarkupLine("[dim]Use --project to specify a project file.[/]");
                    return ExitCodeConstants.FailedToFindProject;
                }
            }

            if (!projectFile.Exists)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Project file not found: {projectFile.FullName}");
                return ExitCodeConstants.FailedToFindProject;
            }

            // Select channel
            var channel = await SelectChannelAsync(projectFile, channelName);
            if (channel == null)
            {
                return ExitCodeConstants.Cancelled;
            }

            AnsiConsole.MarkupLine($"[cyan]Selected channel:[/] {channel.DisplayName}");
            AnsiConsole.WriteLine();

            // Perform upgrade
            var result = await _projectUpdater.UpdateProjectAsync(projectFile, channel, CancellationToken.None);

            if (result.UpdatesApplied || result.TargetFrameworkUpdated)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[green]✓[/] [bold]Upgrade completed successfully[/]");
                
                if (result.TargetFrameworkUpdated)
                {
                    AnsiConsole.MarkupLine($"[dim]Target framework updated to:[/] {result.NewTargetFramework}");
                }
                
                if (result.UpdatedPackages.Count > 0)
                {
                    AnsiConsole.MarkupLine("[dim]Updated packages:[/]");
                    foreach (var package in result.UpdatedPackages)
                    {
                        AnsiConsole.MarkupLine($"  [dim]•[/] {package}");
                    }
                }

                if (channel.Type == MauiChannelType.Nightly)
                {
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("[yellow]Note:[/] You are now using nightly builds. These are pre-release versions and may contain bugs.");
                    AnsiConsole.MarkupLine("[dim]To restore packages, run:[/] dotnet restore");
                }

                return 0;
            }
            else
            {
                return 0;
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return ExitCodeConstants.GeneralError;
        }
    }

    private async Task<MauiChannel?> SelectChannelAsync(FileInfo projectFile, string? channelName)
    {
        var channels = GetAvailableChannels();

        // If channel was specified via command line, use it
        if (!string.IsNullOrEmpty(channelName))
        {
            var selectedChannel = channels.FirstOrDefault(c => 
                c.Name.Equals(channelName, StringComparison.OrdinalIgnoreCase));

            if (selectedChannel == null)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Unknown channel '{channelName}'");
                AnsiConsole.MarkupLine("[dim]Available channels: net9-stable, net10-stable, net9-nightly, net10-nightly[/]");
                return null;
            }

            return selectedChannel;
        }

        // Detect current target framework to provide a recommendation
        var targetFramework = await DetectTargetFrameworkAsync(projectFile);
        var recommendedChannel = GetRecommendedChannel(channels, targetFramework);

        AnsiConsole.MarkupLine("[cyan]Select a channel to upgrade to:[/]");
        AnsiConsole.WriteLine();

        var prompt = new SelectionPrompt<MauiChannel>()
            .Title("Available channels:")
            .PageSize(10)
            .AddChoices(channels)
            .UseConverter(c => 
            {
                var marker = c == recommendedChannel ? " [green](recommended)[/]" : "";
                return $"{c.DisplayName}{marker}";
            });

        var selected = AnsiConsole.Prompt(prompt);
        AnsiConsole.WriteLine();

        return selected;
    }

    private static List<MauiChannel> GetAvailableChannels()
    {
        return new List<MauiChannel>
        {
            MauiChannel.CreateNet9StableChannel(),
            MauiChannel.CreateNet10StableChannel(),
            MauiChannel.CreateNet9NightlyChannel(),
            MauiChannel.CreateNet10NightlyChannel()
        };
    }

    private static MauiChannel? GetRecommendedChannel(List<MauiChannel> channels, string? targetFramework)
    {
        if (string.IsNullOrEmpty(targetFramework))
        {
            return channels.FirstOrDefault(c => c.Name == "net9-stable");
        }

        // Recommend matching stable channel for the detected framework
        if (targetFramework.StartsWith("net9"))
        {
            return channels.FirstOrDefault(c => c.Name == "net9-stable");
        }
        else if (targetFramework.StartsWith("net10"))
        {
            return channels.FirstOrDefault(c => c.Name == "net10-stable");
        }

        return channels.FirstOrDefault(c => c.Name == "net9-stable");
    }

    private static async Task<string?> DetectTargetFrameworkAsync(FileInfo projectFile)
    {
        try
        {
            var content = await File.ReadAllTextAsync(projectFile.FullName);
            
            // Simple regex to extract target framework
            var match = System.Text.RegularExpressions.Regex.Match(content, @"<TargetFrameworks?>(.*?)</TargetFrameworks?>");
            if (match.Success)
            {
                var frameworks = match.Groups[1].Value.Split(';', StringSplitOptions.RemoveEmptyEntries);
                if (frameworks.Length > 0)
                {
                    // Extract net9.0 or net10.0 from net9.0-android, etc.
                    var frameworkMatch = System.Text.RegularExpressions.Regex.Match(frameworks[0], @"(net\d+\.\d+)");
                    if (frameworkMatch.Success)
                    {
                        return frameworkMatch.Groups[1].Value;
                    }
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
