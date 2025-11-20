using System.CommandLine;
using System.Runtime.InteropServices;
using Maui.Cli.Models;
using Maui.Cli.Services;
using Spectre.Console;

namespace Maui.Cli.Commands;

internal sealed class CheckCommand : Command
{
    private readonly IEnvironmentCheckService _checkService;
    private readonly IAnsiConsole _console;

    public CheckCommand(
        IEnvironmentCheckService checkService,
        IAnsiConsole console)
        : base("check", "Diagnose your .NET MAUI development environment")
    {
        _checkService = checkService;
        _console = console;

        var verboseOption = new Option<bool>(
            "--verbose",
            "Show detailed diagnostic information");
        verboseOption.AddAlias("-v");
        AddOption(verboseOption);

        var platformOption = new Option<string?>(
            "--platform",
            "Check specific platform (android, ios, maccatalyst, windows). If not specified, checks all applicable platforms for your OS.");
        platformOption.AddAlias("-p");
        AddOption(platformOption);

        var manifestOption = new Option<string?>(
            "--manifest",
            "Manifest file or URL to use for version requirements. Defaults to aka.ms/dotnet-maui-check-manifest");
        manifestOption.AddAlias("-m");
        AddOption(manifestOption);

        this.SetHandler(ExecuteAsync, verboseOption, platformOption, manifestOption);
    }

    private async Task<int> ExecuteAsync(bool verbose, string? platform, string? manifest)
    {
        _console.Write(new FigletText("MAUI Check").Color(Color.Blue));
        _console.MarkupLine("[dim]Checking your .NET MAUI development environment...[/]");
        _console.WriteLine();

        if (!string.IsNullOrWhiteSpace(manifest))
        {
            _console.MarkupLine($"[dim]Using manifest: {manifest}[/]");
            _console.WriteLine();
        }

        var results = await _checkService.CheckEnvironmentAsync(platform, verbose, manifest);

        _console.WriteLine();
        _console.Write(new Rule("[blue]Diagnostic Summary[/]").RuleStyle(Style.Parse("blue dim")));
        _console.WriteLine();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey);

        table.AddColumn(new TableColumn("[bold]Component[/]").LeftAligned());
        table.AddColumn(new TableColumn("[bold]Status[/]").Centered());
        table.AddColumn(new TableColumn("[bold]Details[/]").LeftAligned());

        foreach (var result in results)
        {
            var statusMarkup = result.Status switch
            {
                CheckStatus.Ok => "[green]✓ OK[/]",
                CheckStatus.Warning => "[yellow]⚠ Warning[/]",
                CheckStatus.Error => "[red]✗ Error[/]",
                CheckStatus.NotApplicable => "[dim]- N/A[/]",
                _ => "[dim]? Unknown[/]"
            };

            table.AddRow(
                result.Name,
                statusMarkup,
                Markup.Escape(result.Message ?? string.Empty));
        }

        _console.Write(table);
        _console.WriteLine();

        // Show recommendations
        var errors = results.Where(r => r.Status == CheckStatus.Error).ToList();
        var warnings = results.Where(r => r.Status == CheckStatus.Warning).ToList();

        if (errors.Any() || warnings.Any())
        {
            _console.Write(new Rule("[yellow]Recommendations[/]").RuleStyle(Style.Parse("yellow dim")));
            _console.WriteLine();

            if (errors.Any())
            {
                _console.MarkupLine("[red bold]Errors Found:[/]");
                foreach (var error in errors)
                {
                    _console.MarkupLine($"  [red]•[/] [bold]{error.Name}[/]: {Markup.Escape(error.Message ?? string.Empty)}");
                    if (!string.IsNullOrEmpty(error.Recommendation))
                    {
                        _console.MarkupLine($"    [dim]→[/] {Markup.Escape(error.Recommendation)}");
                    }
                }
                _console.WriteLine();
            }

            if (warnings.Any())
            {
                _console.MarkupLine("[yellow bold]Warnings:[/]");
                foreach (var warning in warnings)
                {
                    _console.MarkupLine($"  [yellow]•[/] [bold]{warning.Name}[/]: {Markup.Escape(warning.Message ?? string.Empty)}");
                    if (!string.IsNullOrEmpty(warning.Recommendation))
                    {
                        _console.MarkupLine($"    [dim]→[/] {Markup.Escape(warning.Recommendation)}");
                    }
                }
                _console.WriteLine();
            }
        }
        else
        {
            _console.MarkupLine("[green]✓ All checks passed! Your environment is ready for .NET MAUI development.[/]");
            _console.WriteLine();
        }

        // Return appropriate exit code
        return errors.Any() ? ExitCodeConstants.GeneralError : ExitCodeConstants.Success;
    }
}
