namespace Maui.Cli.Models;

internal sealed class WorkloadDependencyInfo
{
    public required string WorkloadName { get; set; }
    public string? Alias { get; set; }
    public string? Version { get; set; }
    
    // Xcode requirements (iOS/macOS)
    public string? XcodeVersion { get; set; }
    public string? XcodeRecommendedVersion { get; set; }
    
    // SDK version (iOS)
    public string? SdkVersion { get; set; }
    
    // JDK requirements (Android)
    public string? JdkVersion { get; set; }
    public string? JdkRecommendedVersion { get; set; }
    
    // Android SDK requirements
    public List<AndroidSdkPackage>? AndroidSdkPackages { get; set; }
}

internal sealed class AndroidSdkPackage
{
    public required string Id { get; set; }
    public required string Description { get; set; }
    public string? RecommendedVersion { get; set; }
    public bool Optional { get; set; }
}
