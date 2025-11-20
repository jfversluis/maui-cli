using System.Text.Json.Serialization;

namespace Maui.Cli.Models;

internal sealed class CheckManifest
{
    [JsonPropertyName("check")]
    public CheckConfiguration? Check { get; init; }
}

internal sealed class CheckConfiguration
{
    [JsonPropertyName("toolVersion")]
    public string? ToolVersion { get; init; }

    [JsonPropertyName("variables")]
    public Dictionary<string, string>? Variables { get; init; }

    [JsonPropertyName("variableMappers")]
    public List<object>? VariableMappers { get; init; }

    [JsonPropertyName("openjdk")]
    public OpenJdkConfiguration? OpenJdk { get; init; }

    [JsonPropertyName("xcode")]
    public XcodeConfiguration? Xcode { get; init; }

    [JsonPropertyName("android")]
    public AndroidConfiguration? Android { get; init; }

    [JsonPropertyName("dotnet")]
    public DotNetConfiguration? DotNet { get; init; }

    [JsonPropertyName("vswin")]
    public VisualStudioConfiguration? VisualStudio { get; init; }
}

internal sealed class OpenJdkConfiguration
{
    [JsonPropertyName("version")]
    public string? Version { get; init; }

    [JsonPropertyName("minimumVersion")]
    public string? MinimumVersion { get; init; }

    [JsonPropertyName("requireExact")]
    public bool RequireExact { get; init; }

    [JsonPropertyName("urls")]
    public Dictionary<string, string>? Urls { get; init; }
}

internal sealed class XcodeConfiguration
{
    [JsonPropertyName("exactVersion")]
    public string? ExactVersion { get; init; }

    [JsonPropertyName("exactVersionName")]
    public string? ExactVersionName { get; init; }

    [JsonPropertyName("minimumVersion")]
    public string? MinimumVersion { get; init; }

    [JsonPropertyName("minimumVersionName")]
    public string? MinimumVersionName { get; init; }
}

internal sealed class AndroidConfiguration
{
    [JsonPropertyName("packages")]
    public List<AndroidPackage>? Packages { get; init; }

    [JsonPropertyName("emulators")]
    public List<AndroidEmulator>? Emulators { get; init; }
}

internal sealed class AndroidPackage
{
    [JsonPropertyName("path")]
    public required string Path { get; init; }

    [JsonPropertyName("version")]
    public required string Version { get; init; }

    [JsonPropertyName("arch")]
    public string? Arch { get; init; }

    [JsonPropertyName("alternatives")]
    public List<AndroidPackage>? Alternatives { get; init; }
}

internal sealed class AndroidEmulator
{
    [JsonPropertyName("sdkId")]
    public string? SdkId { get; init; }

    [JsonPropertyName("alternateSdkIds")]
    public List<string>? AlternateSdkIds { get; init; }

    [JsonPropertyName("desc")]
    public string? Description { get; init; }

    [JsonPropertyName("apiLevel")]
    public int ApiLevel { get; init; }

    [JsonPropertyName("tag")]
    public string? Tag { get; init; }

    [JsonPropertyName("device")]
    public string? Device { get; init; }
}

internal sealed class DotNetConfiguration
{
    [JsonPropertyName("sdks")]
    public List<DotNetSdk>? Sdks { get; init; }
}

internal sealed class DotNetSdk
{
    [JsonPropertyName("version")]
    public string? Version { get; init; }

    [JsonPropertyName("requireExact")]
    public bool RequireExact { get; init; }

    [JsonPropertyName("urls")]
    public Dictionary<string, string>? Urls { get; init; }

    [JsonPropertyName("packageSources")]
    public List<string>? PackageSources { get; init; }

    [JsonPropertyName("workloadRollback")]
    public string? WorkloadRollback { get; init; }

    [JsonPropertyName("workloadIds")]
    public List<string>? WorkloadIds { get; init; }
}

internal sealed class VisualStudioConfiguration
{
    [JsonPropertyName("minimumVersion")]
    public string? MinimumVersion { get; init; }

    [JsonPropertyName("exactVersion")]
    public string? ExactVersion { get; init; }

    [JsonPropertyName("exactVersionName")]
    public string? ExactVersionName { get; init; }
}
