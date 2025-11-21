using System.CommandLine;
using Maui.Cli;
using Maui.Cli.Commands;
using Maui.Cli.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console;

var builder = Host.CreateApplicationBuilder(args);

// Get user's home directory for storing hives and cache
var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
var mauiPath = Path.Combine(homeDirectory, ".maui");
var hivesDirectory = new DirectoryInfo(Path.Combine(mauiPath, "hives"));
var cacheDirectory = new DirectoryInfo(Path.Combine(mauiPath, "cache"));

// Ensure directories exist
Directory.CreateDirectory(hivesDirectory.FullName);
Directory.CreateDirectory(cacheDirectory.FullName);

// Check for debug mode
var debugMode = args.Contains("--debug") || args.Contains("-d");

// Create execution context
var workingDirectory = new DirectoryInfo(Environment.CurrentDirectory);
var context = new CliExecutionContext(workingDirectory, hivesDirectory, cacheDirectory, debugMode);

// Register services
builder.Services.AddSingleton(context);
builder.Services.AddSingleton<IAnsiConsole>(AnsiConsole.Console);
builder.Services.AddHttpClient<IGitHubArtifactService, GitHubArtifactService>();
builder.Services.AddSingleton<INuGetService, NuGetService>();
builder.Services.AddHttpClient<IManifestService, ManifestService>();
builder.Services.AddSingleton<IProcessRunner, ProcessRunner>();
builder.Services.AddSingleton<IWorkloadDependencyService, WorkloadDependencyService>();
builder.Services.AddSingleton<IEnvironmentCheckService, EnvironmentCheckService>();
builder.Services.AddSingleton<INuGetConfigService, NuGetConfigService>();
builder.Services.AddSingleton<IMauiProjectUpdater, MauiProjectUpdater>();
builder.Services.AddSingleton<IProjectLocator, ProjectLocator>();

var app = builder.Build();

// Create root command
var rootCommand = new RootCommand("MAUI CLI - Tools for .NET MAUI developers");

// Add debug option
var debugOption = new Option<bool>("--debug", "Enable debug output");
debugOption.AddAlias("-d");
rootCommand.AddGlobalOption(debugOption);

// Add commands
var applyPrCommand = new ApplyPRCommand(
    app.Services.GetRequiredService<IGitHubArtifactService>(),
    app.Services.GetRequiredService<INuGetService>(),
    app.Services.GetRequiredService<IAnsiConsole>(),
    context);

var checkCommand = new CheckCommand(
    app.Services.GetRequiredService<IEnvironmentCheckService>(),
    app.Services.GetRequiredService<IAnsiConsole>());

var upgradeCommand = new UpgradeCommand(
    app.Services.GetRequiredService<IMauiProjectUpdater>(),
    app.Services.GetRequiredService<IProjectLocator>());

rootCommand.AddCommand(applyPrCommand);
rootCommand.AddCommand(checkCommand);
rootCommand.AddCommand(upgradeCommand);

// Execute
var exitCode = await rootCommand.InvokeAsync(args);
return exitCode;
