using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Maui.Cli.Models;

namespace Maui.Cli.Services;

internal sealed class EnvironmentCheckService : IEnvironmentCheckService
{
    private readonly IManifestService _manifestService;
    private readonly bool _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    private readonly bool _isMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    private readonly bool _isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    private CheckManifest? _manifest;

    public EnvironmentCheckService(IManifestService manifestService)
    {
        _manifestService = manifestService;
    }

    public async Task<IReadOnlyList<CheckResult>> CheckEnvironmentAsync(string? platform, bool verbose, string? manifestUrl = null)
    {
        var results = new List<CheckResult>();

        // Load manifest (from URL if provided, otherwise use default/embedded)
        _manifest = await _manifestService.LoadManifestAsync(manifestUrl);

        // Check .NET SDK
        results.Add(await CheckDotNetSdkAsync(verbose));

        // Check .NET MAUI workloads
        results.AddRange(await CheckMauiWorkloadsAsync(platform, verbose));

        // Platform-specific checks
        if (ShouldCheckAndroid(platform))
        {
            results.Add(await CheckJavaJdkAsync(verbose));
            results.Add(await CheckAndroidSdkAsync(verbose));
        }

        if (ShouldCheckIos(platform))
        {
            results.Add(await CheckXcodeAsync(verbose));
        }

        if (ShouldCheckWindows(platform))
        {
            results.Add(CheckWindowsSdk());
        }

        return results;
    }

    private bool ShouldCheckAndroid(string? platform)
        => platform is null or "android";

    private bool ShouldCheckIos(string? platform)
        => (platform is null or "ios" or "maccatalyst") && _isMacOS;

    private bool ShouldCheckWindows(string? platform)
        => (platform is null or "windows") && _isWindows;

    private async Task<CheckResult> CheckDotNetSdkAsync(bool verbose)
    {
        try
        {
            var result = await RunProcessAsync("dotnet", "--version");
            if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Output))
            {
                var version = result.Output.Trim();
                var versionParts = version.Split('.');
                
                Dictionary<string, string>? details = null;

                if (verbose)
                {
                    details = new Dictionary<string, string> { ["Version"] = version };

                    // Get more detailed SDK information
                    var infoResult = await RunProcessAsync("dotnet", "--info");
                    if (infoResult.ExitCode == 0 && !string.IsNullOrWhiteSpace(infoResult.Output))
                    {
                        var info = ParseDotNetInfo(infoResult.Output);
                        foreach (var (key, value) in info)
                        {
                            details[key] = value;
                        }
                    }

                    // Get list of installed SDKs
                    var sdkListResult = await RunProcessAsync("dotnet", "--list-sdks");
                    if (sdkListResult.ExitCode == 0)
                    {
                        var sdkCount = sdkListResult.Output?.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length ?? 0;
                        details["InstalledSDKs"] = sdkCount.ToString();
                    }
                }
                
                if (versionParts.Length >= 1 && int.TryParse(versionParts[0], out var majorVersion))
                {
                    if (majorVersion >= 8)
                    {
                        var message = verbose && details != null && details.ContainsKey("RuntimeVersion")
                            ? $"Version {version} (Runtime: {details["RuntimeVersion"]})"
                            : $"Version {version}";

                        return new CheckResult
                        {
                            Name = ".NET SDK",
                            Status = CheckStatus.Ok,
                            Message = message,
                            Details = details
                        };
                    }
                    else
                    {
                        return new CheckResult
                        {
                            Name = ".NET SDK",
                            Status = CheckStatus.Warning,
                            Message = $"Version {version} detected. .NET 8 or later recommended.",
                            Recommendation = "Install .NET 8 or .NET 9 SDK from https://dot.net",
                            Details = details
                        };
                    }
                }
            }

            return new CheckResult
            {
                Name = ".NET SDK",
                Status = CheckStatus.Error,
                Message = "Not found or version could not be determined",
                Recommendation = "Install .NET 8 or later from https://dot.net"
            };
        }
        catch (Exception ex)
        {
            return new CheckResult
            {
                Name = ".NET SDK",
                Status = CheckStatus.Error,
                Message = "Not found",
                Recommendation = $"Install .NET 8 or later from https://dot.net. Error: {ex.Message}"
            };
        }
    }

    private Dictionary<string, string> ParseDotNetInfo(string output)
    {
        var info = new Dictionary<string, string>();
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var line in lines)
        {
            if (line.Contains("Version:", StringComparison.OrdinalIgnoreCase))
            {
                var match = Regex.Match(line, @"Version:\s*(.+)");
                if (match.Success && !info.ContainsKey("RuntimeVersion"))
                {
                    info["RuntimeVersion"] = match.Groups[1].Value.Trim();
                }
            }
            else if (line.Contains("Commit:", StringComparison.OrdinalIgnoreCase))
            {
                var match = Regex.Match(line, @"Commit:\s*(.+)");
                if (match.Success)
                {
                    info["Commit"] = match.Groups[1].Value.Trim();
                }
            }
            else if (line.Contains("RID:", StringComparison.OrdinalIgnoreCase))
            {
                var match = Regex.Match(line, @"RID:\s*(.+)");
                if (match.Success)
                {
                    info["RID"] = match.Groups[1].Value.Trim();
                }
            }
            else if (line.Contains("Base Path:", StringComparison.OrdinalIgnoreCase))
            {
                var match = Regex.Match(line, @"Base Path:\s*(.+)");
                if (match.Success)
                {
                    info["BasePath"] = match.Groups[1].Value.Trim();
                }
            }
        }

        return info;
    }

    private async Task<List<CheckResult>> CheckMauiWorkloadsAsync(string? platform, bool verbose)
    {
        var results = new List<CheckResult>();
        
        try
        {
            // Try JSON format first (available in newer SDKs)
            var jsonResult = await RunProcessAsync("dotnet", "workload list --format json");
            Dictionary<string, WorkloadInfo> installedWorkloads;
            string? debugInfo = null;

            if (jsonResult.ExitCode == 0 && !string.IsNullOrWhiteSpace(jsonResult.Output))
            {
                try
                {
                    installedWorkloads = ParseWorkloadsJson(jsonResult.Output);
                    if (verbose)
                    {
                        debugInfo = $"Found {installedWorkloads.Count} workloads via JSON: {string.Join(", ", installedWorkloads.Keys)}";
                    }
                }
                catch (Exception ex)
                {
                    // Fall back to text parsing if JSON fails
                    var textResult = await RunProcessAsync("dotnet", "workload list");
                    installedWorkloads = ParseWorkloadsText(textResult.Output ?? string.Empty);
                    if (verbose)
                    {
                        debugInfo = $"JSON parse failed ({ex.Message}), used text parsing. Found {installedWorkloads.Count} workloads: {string.Join(", ", installedWorkloads.Keys)}";
                    }
                }
            }
            else
            {
                // Fall back to text format for older SDKs
                var textResult = await RunProcessAsync("dotnet", "workload list");
                if (textResult.ExitCode != 0)
                {
                    results.Add(new CheckResult
                    {
                        Name = "MAUI Workloads",
                        Status = CheckStatus.Error,
                        Message = "Could not query workloads",
                        Recommendation = "Ensure .NET SDK is properly installed",
                        Details = verbose ? new Dictionary<string, string> { ["Error"] = textResult.Error ?? "Unknown error" } : null
                    });
                    return results;
                }
                installedWorkloads = ParseWorkloadsText(textResult.Output ?? string.Empty);
                if (verbose)
                {
                    debugInfo = $"Found {installedWorkloads.Count} workloads via text: {string.Join(", ", installedWorkloads.Keys)}";
                }
            }

            // Required workloads based on platform
            // Note: .NET 9 and earlier use "maui-android", "maui-ios", etc.
            // .NET 10+ uses "android", "ios", "maccatalyst", "maui-windows"
            var requiredWorkloads = new Dictionary<string, (string[] PossibleIds, string DisplayName)>();
            
            if (platform is null or "android")
                requiredWorkloads["android"] = (new[] { "android", "maui-android" }, "Android");
            
            if (platform is null or "ios" && _isMacOS)
                requiredWorkloads["ios"] = (new[] { "ios", "maui-ios" }, "iOS");
            
            if (platform is null or "maccatalyst" && _isMacOS)
                requiredWorkloads["maccatalyst"] = (new[] { "maccatalyst", "maui-maccatalyst" }, "Mac Catalyst");
            
            if (platform is null or "windows" && _isWindows)
                requiredWorkloads["windows"] = (new[] { "maui-windows", "windows" }, "Windows");

            foreach (var (key, (possibleIds, platformName)) in requiredWorkloads)
            {
                // Try to find any of the possible workload IDs
                WorkloadInfo? workloadInfo = null;
                string? foundWorkloadId = null;
                
                foreach (var workloadId in possibleIds)
                {
                    if (installedWorkloads.TryGetValue(workloadId, out workloadInfo))
                    {
                        foundWorkloadId = workloadId;
                        break;
                    }
                }
                
                if (workloadInfo != null && foundWorkloadId != null)
                {
                    var message = verbose && !string.IsNullOrEmpty(workloadInfo.ManifestVersion)
                        ? $"Installed (version {workloadInfo.Version}, manifest {workloadInfo.ManifestVersion})"
                        : $"Installed (version {workloadInfo.Version})";

                    var details = verbose ? new Dictionary<string, string>
                    {
                        ["WorkloadId"] = foundWorkloadId,
                        ["Version"] = workloadInfo.Version,
                        ["ManifestVersion"] = workloadInfo.ManifestVersion ?? "N/A",
                        ["Description"] = workloadInfo.Description ?? "N/A"
                    } : null;
                    
                    if (verbose && debugInfo != null && !details!.ContainsKey("Debug"))
                    {
                        details["Debug"] = debugInfo;
                    }

                    results.Add(new CheckResult
                    {
                        Name = $"MAUI Workload ({platformName})",
                        Status = CheckStatus.Ok,
                        Message = message,
                        Details = details
                    });
                }
                else
                {
                    var details = verbose ? new Dictionary<string, string>
                    {
                        ["ExpectedWorkloadIds"] = string.Join(" or ", possibleIds),
                        ["InstalledWorkloads"] = string.Join(", ", installedWorkloads.Keys)
                    } : null;
                    
                    if (verbose && debugInfo != null)
                    {
                        details!["Debug"] = debugInfo;
                    }

                    // Recommend the first (most modern) ID for installation
                    var recommendedId = possibleIds[0];
                    results.Add(new CheckResult
                    {
                        Name = $"MAUI Workload ({platformName})",
                        Status = CheckStatus.Error,
                        Message = "Not installed",
                        Recommendation = $"Run: dotnet workload install {recommendedId}",
                        Details = details
                    });
                }
            }

            // Check for the base MAUI workload
            if (!installedWorkloads.ContainsKey("maui") && 
                !installedWorkloads.Any(w => w.Key.StartsWith("maui-")))
            {
                results.Add(new CheckResult
                {
                    Name = "MAUI Workloads",
                    Status = CheckStatus.Error,
                    Message = "No MAUI workloads installed",
                    Recommendation = "Run: dotnet workload install maui"
                });
            }
        }
        catch (Exception ex)
        {
            results.Add(new CheckResult
            {
                Name = "MAUI Workloads",
                Status = CheckStatus.Error,
                Message = "Could not check workloads",
                Recommendation = $"Error: {ex.Message}"
            });
        }

        return results;
    }

    private async Task<CheckResult> CheckJavaJdkAsync(bool verbose)
    {
        var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
        
        // Get required JDK version from manifest
        var requiredVersion = "17.0";
        var requiredMajorVersion = 17;
        
        if (_manifest?.Check?.OpenJdk?.Version != null)
        {
            requiredVersion = _manifest.Check.OpenJdk.Version;
            var parts = requiredVersion.Split('.');
            if (parts.Length > 0 && int.TryParse(parts[0], out var maj))
            {
                requiredMajorVersion = maj;
            }
        }
        
        if (string.IsNullOrWhiteSpace(javaHome) || !Directory.Exists(javaHome))
        {
            return new CheckResult
            {
                Name = "Java JDK",
                Status = CheckStatus.Error,
                Message = "JAVA_HOME not set or directory not found",
                Recommendation = _isMacOS 
                    ? $"Install JDK {requiredVersion} from https://learn.microsoft.com/dotnet/android/getting-started/installation/dependencies or set JAVA_HOME"
                    : $"Install JDK {requiredVersion} or later and set JAVA_HOME environment variable"
            };
        }

        try
        {
            var javaExe = _isWindows ? "java.exe" : "java";
            var javaPath = Path.Combine(javaHome, "bin", javaExe);
            
            if (!File.Exists(javaPath))
            {
                return new CheckResult
                {
                    Name = "Java JDK",
                    Status = CheckStatus.Warning,
                    Message = $"JAVA_HOME is set but java executable not found at {javaPath}",
                    Recommendation = "Verify JAVA_HOME points to a valid JDK installation"
                };
            }

            var result = await RunProcessAsync(javaPath, "-version");
            
            if (result.ExitCode == 0)
            {
                // Java outputs version to stderr
                var versionOutput = result.Error ?? result.Output ?? string.Empty;
                var versionMatch = Regex.Match(versionOutput, @"version\s+""?(\d+)");
                
                if (versionMatch.Success && int.TryParse(versionMatch.Groups[1].Value, out var majorVersion))
                {
                    if (majorVersion >= requiredMajorVersion)
                    {
                        return new CheckResult
                        {
                            Name = "Java JDK",
                            Status = CheckStatus.Ok,
                            Message = $"Version {majorVersion} (JAVA_HOME: {javaHome})"
                        };
                    }
                    else if (majorVersion >= 11)
                    {
                        return new CheckResult
                        {
                            Name = "Java JDK",
                            Status = CheckStatus.Warning,
                            Message = $"Version {majorVersion} detected. JDK {requiredMajorVersion}+ recommended for best compatibility.",
                            Recommendation = $"Install JDK {requiredVersion} or later from https://learn.microsoft.com/dotnet/android/getting-started/installation/dependencies"
                        };
                    }
                    else
                    {
                        return new CheckResult
                        {
                            Name = "Java JDK",
                            Status = CheckStatus.Error,
                            Message = $"Version {majorVersion} detected. JDK {requiredMajorVersion}+ required.",
                            Recommendation = $"Install JDK {requiredVersion} or later from https://learn.microsoft.com/dotnet/android/getting-started/installation/dependencies"
                        };
                    }
                }
            }

            return new CheckResult
            {
                Name = "Java JDK",
                Status = CheckStatus.Ok,
                Message = $"Found at {javaHome}",
                Recommendation = null
            };
        }
        catch (Exception ex)
        {
            return new CheckResult
            {
                Name = "Java JDK",
                Status = CheckStatus.Error,
                Message = "Could not verify Java installation",
                Recommendation = $"Error: {ex.Message}"
            };
        }
    }

    private async Task<CheckResult> CheckAndroidSdkAsync(bool verbose)
    {
        var androidHome = Environment.GetEnvironmentVariable("ANDROID_HOME") 
                         ?? Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT");
        
        if (string.IsNullOrWhiteSpace(androidHome))
        {
            // Try default locations
            if (_isMacOS)
            {
                var defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
                    "Library", "Android", "sdk");
                if (Directory.Exists(defaultPath))
                    androidHome = defaultPath;
            }
            else if (_isWindows)
            {
                var defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                    "Android", "Sdk");
                if (Directory.Exists(defaultPath))
                    androidHome = defaultPath;
            }
        }

        if (string.IsNullOrWhiteSpace(androidHome) || !Directory.Exists(androidHome))
        {
            return new CheckResult
            {
                Name = "Android SDK",
                Status = CheckStatus.Error,
                Message = "ANDROID_HOME or ANDROID_SDK_ROOT not set, or directory not found",
                Recommendation = "Install Android SDK through Android Studio or Visual Studio, then set ANDROID_HOME environment variable. See: https://learn.microsoft.com/dotnet/android/getting-started/installation/dependencies"
            };
        }

        var platformToolsPath = Path.Combine(androidHome, "platform-tools");
        var buildToolsPath = Path.Combine(androidHome, "build-tools");
        var platformsPath = Path.Combine(androidHome, "platforms");

        var missingComponents = new List<string>();
        if (!Directory.Exists(platformToolsPath)) missingComponents.Add("platform-tools");
        if (!Directory.Exists(buildToolsPath)) missingComponents.Add("build-tools");
        if (!Directory.Exists(platformsPath)) missingComponents.Add("platforms");

        if (missingComponents.Any())
        {
            return new CheckResult
            {
                Name = "Android SDK",
                Status = CheckStatus.Warning,
                Message = $"Found at {androidHome}, but missing components: {string.Join(", ", missingComponents)}",
                Recommendation = "Use Android SDK Manager to install missing components (API 21+ required)"
            };
        }

        // Check for minimum platform from manifest
        var minApiLevel = 21; // Default minimum
        var targetApiLevel = 34; // Default target
        
        if (_manifest?.Check?.Variables != null)
        {
            if (_manifest.Check.Variables.TryGetValue("MIN_ANDROID_API", out var minApi) &&
                int.TryParse(minApi, out var parsedMinApi))
            {
                minApiLevel = parsedMinApi;
            }
            
            if (_manifest.Check.Variables.TryGetValue("TARGET_ANDROID_API", out var targetApi) &&
                int.TryParse(targetApi, out var parsedTargetApi))
            {
                targetApiLevel = parsedTargetApi;
            }
        }

        var platformDirs = Directory.GetDirectories(platformsPath);
        var hasMinimumApi = platformDirs.Any(d =>
        {
            var dirName = Path.GetFileName(d);
            var match = Regex.Match(dirName, @"android-(\d+)");
            return match.Success && int.TryParse(match.Groups[1].Value, out var apiLevel) && apiLevel >= minApiLevel;
        });

        var hasTargetApi = platformDirs.Any(d =>
        {
            var dirName = Path.GetFileName(d);
            var match = Regex.Match(dirName, @"android-(\d+)");
            return match.Success && int.TryParse(match.Groups[1].Value, out var apiLevel) && apiLevel >= targetApiLevel;
        });

        if (!hasMinimumApi)
        {
            return new CheckResult
            {
                Name = "Android SDK",
                Status = CheckStatus.Error,
                Message = $"Found at {androidHome}, but no platforms API {minApiLevel}+ detected",
                Recommendation = $"Install Android SDK Platform API {minApiLevel} or later using Android SDK Manager"
            };
        }

        if (!hasTargetApi && verbose)
        {
            return new CheckResult
            {
                Name = "Android SDK",
                Status = CheckStatus.Warning,
                Message = $"Found at {androidHome}. Minimum API {minApiLevel} found, but target API {targetApiLevel} recommended",
                Recommendation = $"Install Android SDK Platform API {targetApiLevel} for latest features and Google Play compatibility"
            };
        }

        return new CheckResult
        {
            Name = "Android SDK",
            Status = CheckStatus.Ok,
            Message = $"Found at {androidHome}"
        };
    }

    private async Task<CheckResult> CheckXcodeAsync(bool verbose)
    {
        if (!_isMacOS)
        {
            return new CheckResult
            {
                Name = "Xcode",
                Status = CheckStatus.NotApplicable,
                Message = "Not applicable on this platform"
            };
        }

        try
        {
            var result = await RunProcessAsync("xcode-select", "-p");
            
            if (result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.Output))
            {
                return new CheckResult
                {
                    Name = "Xcode",
                    Status = CheckStatus.Error,
                    Message = "Xcode command line tools not found",
                    Recommendation = "Install Xcode from the App Store and run: sudo xcode-select --switch /Applications/Xcode.app"
                };
            }

            var developerPath = result.Output.Trim();
            
            // Check Xcode version
            var versionResult = await RunProcessAsync("xcodebuild", "-version");
            if (versionResult.ExitCode == 0 && !string.IsNullOrWhiteSpace(versionResult.Output))
            {
                var versionLine = versionResult.Output.Split('\n')[0];
                var versionMatch = Regex.Match(versionLine, @"Xcode\s+([\d.]+)");
                
                if (versionMatch.Success)
                {
                    var versionString = versionMatch.Groups[1].Value;
                    var parts = versionString.Split('.');
                    if (parts.Length > 0 && int.TryParse(parts[0], out var majorVersion))
                    {
                        // Get minimum version from manifest
                        var minVersion = 15; // Default
                        var minVersionName = "15.0";
                        
                        if (_manifest?.Check?.Xcode != null)
                        {
                            if (!string.IsNullOrEmpty(_manifest.Check.Xcode.MinimumVersion) &&
                                int.TryParse(_manifest.Check.Xcode.MinimumVersion, out var manifestMin))
                            {
                                minVersion = manifestMin;
                                minVersionName = _manifest.Check.Xcode.MinimumVersionName ?? $"{minVersion}.0";
                            }
                            
                            // Check for exact version requirement
                            if (!string.IsNullOrEmpty(_manifest.Check.Xcode.ExactVersionName))
                            {
                                var exactName = _manifest.Check.Xcode.ExactVersionName;
                                if (versionString != exactName)
                                {
                                    return new CheckResult
                                    {
                                        Name = "Xcode",
                                        Status = CheckStatus.Warning,
                                        Message = $"Version {versionString} found. Version {exactName} is recommended for this .NET MAUI version.",
                                        Recommendation = $"Update Xcode to version {exactName} for optimal compatibility"
                                    };
                                }
                            }
                        }

                        if (majorVersion >= minVersion)
                        {
                            return new CheckResult
                            {
                                Name = "Xcode",
                                Status = CheckStatus.Ok,
                                Message = $"Version {versionString} at {developerPath}"
                            };
                        }
                        else
                        {
                            return new CheckResult
                            {
                                Name = "Xcode",
                                Status = CheckStatus.Error,
                                Message = $"Version {versionString} detected. Xcode {minVersionName}+ required",
                                Recommendation = $"Update Xcode to version {minVersionName} or later from the App Store"
                            };
                        }
                    }
                }
            }

            return new CheckResult
            {
                Name = "Xcode",
                Status = CheckStatus.Ok,
                Message = $"Found at {developerPath}"
            };
        }
        catch (Exception ex)
        {
            return new CheckResult
            {
                Name = "Xcode",
                Status = CheckStatus.Error,
                Message = "Could not verify Xcode installation",
                Recommendation = $"Error: {ex.Message}"
            };
        }
    }

    private CheckResult CheckWindowsSdk()
    {
        if (!_isWindows)
        {
            return new CheckResult
            {
                Name = "Windows SDK",
                Status = CheckStatus.NotApplicable,
                Message = "Not applicable on this platform"
            };
        }

        // Check Windows 10/11 version
        var osVersion = Environment.OSVersion.Version;
        
        if (osVersion.Major >= 10)
        {
            // Check for Windows 11 or Windows 10 1809+
            var build = osVersion.Build;
            
            if (build >= 17763) // Windows 10 1809
            {
                return new CheckResult
                {
                    Name = "Windows SDK",
                    Status = CheckStatus.Ok,
                    Message = $"Windows {osVersion.Major}.0 Build {build}"
                };
            }
            else
            {
                return new CheckResult
                {
                    Name = "Windows SDK",
                    Status = CheckStatus.Warning,
                    Message = $"Windows 10 Build {build} detected",
                    Recommendation = "Windows 10 version 1809 (build 17763) or later recommended for .NET MAUI"
                };
            }
        }

        return new CheckResult
        {
            Name = "Windows SDK",
            Status = CheckStatus.Error,
            Message = $"Windows {osVersion.Major} detected",
            Recommendation = "Windows 10 version 1809 or Windows 11 required for .NET MAUI"
        };
    }

    private Dictionary<string, WorkloadInfo> ParseWorkloadsJson(string jsonOutput)
    {
        var workloads = new Dictionary<string, WorkloadInfo>(StringComparer.OrdinalIgnoreCase);
        
        try
        {
            var jsonDoc = JsonDocument.Parse(jsonOutput);
            var root = jsonDoc.RootElement;

            // Handle different JSON structures
            if (root.TryGetProperty("installed", out var installedElement))
            {
                foreach (var workload in installedElement.EnumerateArray())
                {
                    var id = workload.GetProperty("id").GetString();
                    if (string.IsNullOrEmpty(id)) continue;

                    var info = new WorkloadInfo
                    {
                        Version = workload.TryGetProperty("version", out var ver) ? ver.GetString() : "unknown",
                        ManifestVersion = workload.TryGetProperty("manifestVersion", out var manVer) ? manVer.GetString() : null,
                        Description = workload.TryGetProperty("description", out var desc) ? desc.GetString() : null
                    };

                    workloads[id] = info;
                }
            }
            // Alternative structure: array of workloads
            else if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var workload in root.EnumerateArray())
                {
                    var id = workload.GetProperty("id").GetString();
                    if (string.IsNullOrEmpty(id)) continue;

                    var info = new WorkloadInfo
                    {
                        Version = workload.TryGetProperty("version", out var ver) ? ver.GetString() : "unknown",
                        ManifestVersion = workload.TryGetProperty("manifestVersion", out var manVer) ? manVer.GetString() : null,
                        Description = workload.TryGetProperty("description", out var desc) ? desc.GetString() : null
                    };

                    workloads[id] = info;
                }
            }
        }
        catch (JsonException)
        {
            // If JSON parsing fails, return empty dictionary
            // Caller will fall back to text parsing
        }

        return workloads;
    }

    private Dictionary<string, WorkloadInfo> ParseWorkloadsText(string output)
    {
        var workloads = new Dictionary<string, WorkloadInfo>(StringComparer.OrdinalIgnoreCase);
        
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var inWorkloadSection = false;

        foreach (var line in lines)
        {
            // Skip header lines and section markers
            if (line.Contains("Installed Workload", StringComparison.OrdinalIgnoreCase) || 
                line.Contains("---") || 
                line.Contains("Workload ID", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Manifest Version", StringComparison.OrdinalIgnoreCase) ||
                line.All(c => c == '-' || c == ' '))
            {
                inWorkloadSection = true;
                continue;
            }

            // Skip informational lines
            if (line.Contains("Use `dotnet workload search`", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("available workloads", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("No workloads installed", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!inWorkloadSection)
                continue;

            // Parse workload lines (format: "workload-id   version   manifest-version" or just "workload-id")
            var parts = Regex.Split(line, @"\s{2,}").Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToArray();
            if (parts.Length >= 1)
            {
                var workloadId = parts[0];
                
                // Skip if this looks like a footer or info line
                if (workloadId.StartsWith("Use ", StringComparison.OrdinalIgnoreCase) ||
                    workloadId.Contains("update", StringComparison.OrdinalIgnoreCase) ||
                    workloadId.Contains("available", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                
                var info = new WorkloadInfo
                {
                    Version = parts.Length > 1 ? parts[1] : "unknown",
                    ManifestVersion = parts.Length > 2 ? parts[2] : null,
                    Description = null
                };

                workloads[workloadId] = info;
            }
        }

        return workloads;
    }

    private async Task<ProcessResult> RunProcessAsync(string fileName, string arguments)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processStartInfo };
        
        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                outputBuilder.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                errorBuilder.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();

        return new ProcessResult
        {
            ExitCode = process.ExitCode,
            Output = outputBuilder.ToString(),
            Error = errorBuilder.ToString()
        };
    }

    private sealed class ProcessResult
    {
        public required int ExitCode { get; init; }
        public required string Output { get; init; }
        public required string Error { get; init; }
    }

    private sealed class WorkloadInfo
    {
        public required string Version { get; init; }
        public string? ManifestVersion { get; init; }
        public string? Description { get; init; }
    }
}
