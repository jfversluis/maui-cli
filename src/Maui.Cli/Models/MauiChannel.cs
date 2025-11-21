namespace Maui.Cli.Models;

/// <summary>
/// Represents a MAUI package distribution channel (stable, nightly .NET 9, nightly .NET 10, etc.)
/// </summary>
internal sealed class MauiChannel
{
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string FeedUrl { get; init; } = string.Empty;
    public string TargetFramework { get; init; } = string.Empty;
    public MauiChannelType Type { get; init; }

    public static MauiChannel CreateNet9StableChannel()
    {
        return new MauiChannel
        {
            Name = "net9-stable",
            DisplayName = ".NET 9 Stable (Latest MAUI for .NET 9)",
            FeedUrl = "https://api.nuget.org/v3/index.json",
            TargetFramework = "net9.0",
            Type = MauiChannelType.Stable
        };
    }

    public static MauiChannel CreateNet10StableChannel()
    {
        return new MauiChannel
        {
            Name = "net10-stable",
            DisplayName = ".NET 10 Stable (Latest MAUI for .NET 10) + Upgrade TFM",
            FeedUrl = "https://api.nuget.org/v3/index.json",
            TargetFramework = "net10.0",
            Type = MauiChannelType.Stable
        };
    }

    public static MauiChannel CreateNet9NightlyChannel()
    {
        return new MauiChannel
        {
            Name = "net9-nightly",
            DisplayName = ".NET 9 Nightly (Azure DevOps)",
            FeedUrl = "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet9/nuget/v3/index.json",
            TargetFramework = "net9.0",
            Type = MauiChannelType.Nightly
        };
    }

    public static MauiChannel CreateNet10NightlyChannel()
    {
        return new MauiChannel
        {
            Name = "net10-nightly",
            DisplayName = ".NET 10 Nightly (Azure DevOps)",
            FeedUrl = "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet10/nuget/v3/index.json",
            TargetFramework = "net10.0",
            Type = MauiChannelType.Nightly
        };
    }
}

internal enum MauiChannelType
{
    Stable,
    Nightly
}
