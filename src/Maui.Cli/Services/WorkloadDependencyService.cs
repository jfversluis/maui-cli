using System.Text.Json;
using System.Text.Json.Nodes;
using Maui.Cli.Models;

namespace Maui.Cli.Services;

internal sealed class WorkloadDependencyService : IWorkloadDependencyService
{
    private readonly IProcessRunner _processRunner;

    public WorkloadDependencyService(IProcessRunner processRunner)
    {
        _processRunner = processRunner;
    }

    public async Task<WorkloadDependencyInfo?> GetWorkloadDependenciesAsync(string workloadName, string? sdkVersion = null)
    {
        var sdkManifestPath = await GetSdkManifestPathAsync(workloadName, sdkVersion);
        if (sdkManifestPath == null)
        {
            return null;
        }

        var dependenciesPath = Path.Combine(sdkManifestPath, "WorkloadDependencies.json");
        if (!File.Exists(dependenciesPath))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(dependenciesPath);
            var jsonDoc = JsonNode.Parse(json);
            if (jsonDoc == null)
            {
                return null;
            }

            // Find the workload key (e.g., "microsoft.net.sdk.android", "microsoft.net.sdk.ios")
            var workloadKey = jsonDoc.AsObject().FirstOrDefault().Key;
            if (string.IsNullOrEmpty(workloadKey))
            {
                return null;
            }

            var workloadNode = jsonDoc[workloadKey];
            if (workloadNode == null)
            {
                return null;
            }

            return ParseWorkloadDependency(workloadName, workloadNode);
        }
        catch
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<WorkloadDependencyInfo>> GetAllWorkloadDependenciesAsync(string? sdkVersion = null)
    {
        var results = new List<WorkloadDependencyInfo>();

        var workloadNames = new[]
        {
            "microsoft.net.sdk.android",
            "microsoft.net.sdk.ios",
            "microsoft.net.sdk.maccatalyst",
            "microsoft.net.sdk.macos",
            "microsoft.net.sdk.tvos",
            "microsoft.net.sdk.maui"
        };

        foreach (var workloadName in workloadNames)
        {
            var info = await GetWorkloadDependenciesAsync(workloadName, sdkVersion);
            if (info != null)
            {
                results.Add(info);
            }
        }

        return results;
    }

    private async Task<string?> GetSdkManifestPathAsync(string workloadName, string? sdkVersion)
    {
        var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT")
            ?? (OperatingSystem.IsWindows() ? @"C:\Program Files\dotnet" : "/usr/local/share/dotnet");

        var sdkManifestsPath = Path.Combine(dotnetRoot, "sdk-manifests");
        if (!Directory.Exists(sdkManifestsPath))
        {
            return null;
        }

        // If sdkVersion is not provided, get the current SDK version
        if (string.IsNullOrEmpty(sdkVersion))
        {
            var result = await _processRunner.RunProcessAsync("dotnet", "--version", Environment.CurrentDirectory);
            if (result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.Output))
            {
                return null;
            }
            sdkVersion = result.Output.Trim();
        }

        // Determine SDK band (e.g., "10.0.100" from "10.0.101")
        var versionParts = sdkVersion.Split('.');
        if (versionParts.Length < 3)
        {
            return null;
        }

        // Try exact band first, then fallback to feature bands
        var majorMinor = $"{versionParts[0]}.{versionParts[1]}";
        var patch = versionParts[2].Split('-')[0]; // Remove any preview suffix
        var bands = new[] 
        { 
            $"{majorMinor}.{patch}",  // Exact version
            $"{majorMinor}.100",       // RTM band
            $"{majorMinor}.100-rc.2",  // RC band
            $"{majorMinor}.100-rc.1",
            $"{majorMinor}.100-preview.7"
        };

        foreach (var band in bands)
        {
            var bandPath = Path.Combine(sdkManifestsPath, band, workloadName.ToLowerInvariant());
            if (Directory.Exists(bandPath))
            {
                // Find the latest version directory
                var versionDirs = Directory.GetDirectories(bandPath)
                    .Select(d => new DirectoryInfo(d))
                    .OrderByDescending(d => d.Name)
                    .ToList();

                if (versionDirs.Count > 0)
                {
                    return versionDirs[0].FullName;
                }
            }
        }

        return null;
    }

    private WorkloadDependencyInfo ParseWorkloadDependency(string workloadName, JsonNode workloadNode)
    {
        var info = new WorkloadDependencyInfo
        {
            WorkloadName = workloadName
        };

        // Parse workload info
        var workloadInfo = workloadNode["workload"];
        if (workloadInfo != null)
        {
            info.Version = workloadInfo["version"]?.GetValue<string>();
            var aliasArray = workloadInfo["alias"]?.AsArray();
            if (aliasArray != null && aliasArray.Count > 0)
            {
                info.Alias = aliasArray[0]?.GetValue<string>();
            }
        }

        // Parse Xcode requirements (for iOS/macOS workloads)
        var xcodeInfo = workloadNode["xcode"];
        if (xcodeInfo != null)
        {
            info.XcodeVersion = xcodeInfo["version"]?.GetValue<string>();
            info.XcodeRecommendedVersion = xcodeInfo["recommendedVersion"]?.GetValue<string>();
        }

        // Parse JDK requirements (for Android workload)
        var jdkInfo = workloadNode["jdk"];
        if (jdkInfo != null)
        {
            info.JdkVersion = jdkInfo["version"]?.GetValue<string>();
            info.JdkRecommendedVersion = jdkInfo["recommendedVersion"]?.GetValue<string>();
        }

        // Parse Android SDK requirements
        var androidSdkInfo = workloadNode["androidsdk"];
        if (androidSdkInfo != null)
        {
            var packages = androidSdkInfo["packages"]?.AsArray();
            if (packages != null)
            {
                info.AndroidSdkPackages = new List<AndroidSdkPackage>();
                foreach (var package in packages)
                {
                    if (package == null) continue;

                    var sdkPackage = package["sdkPackage"];
                    if (sdkPackage == null) continue;

                    var id = sdkPackage["id"];
                    var packageId = id?.GetValueKind() == System.Text.Json.JsonValueKind.String
                        ? id.GetValue<string>()
                        : null;

                    if (string.IsNullOrEmpty(packageId))
                    {
                        // Handle platform-specific IDs
                        if (id != null && id.GetValueKind() == System.Text.Json.JsonValueKind.Object)
                        {
                            var platform = OperatingSystem.IsWindows() ? "win-x64"
                                : OperatingSystem.IsMacOS() ? (System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64 ? "mac-arm64" : "mac-x64")
                                : "linux-x64";

                            packageId = id[platform]?.GetValue<string>();
                        }
                    }

                    if (!string.IsNullOrEmpty(packageId))
                    {
                        info.AndroidSdkPackages.Add(new AndroidSdkPackage
                        {
                            Description = package["desc"]?.GetValue<string>() ?? packageId,
                            Id = packageId,
                            RecommendedVersion = sdkPackage["recommendedVersion"]?.GetValue<string>(),
                            Optional = package["optional"]?.GetValue<string>() == "true"
                        });
                    }
                }
            }
        }

        // Parse SDK version (for iOS workloads)
        var sdkInfo = workloadNode["sdk"];
        if (sdkInfo != null)
        {
            info.SdkVersion = sdkInfo["version"]?.GetValue<string>();
        }

        return info;
    }
}
