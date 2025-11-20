using System.Reflection;
using System.Text.Json;
using Maui.Cli.Models;

namespace Maui.Cli.Services;

internal sealed class ManifestService : IManifestService
{
    private readonly HttpClient _httpClient;
    
    // Official manifest URL - update when Microsoft publishes one
    private const string DefaultManifestUrl = "https://aka.ms/dotnet-maui-check-manifest";

    public ManifestService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CheckManifest?> LoadManifestAsync(string? manifestUrl = null)
    {
        // Determine which manifest to load
        var url = manifestUrl ?? DefaultManifestUrl;

        // Try to load from URL or file
        try
        {
            string json;
            
            // Check if it's a URL or file path
            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                // Load from URL
                json = await _httpClient.GetStringAsync(url);
            }
            else
            {
                // Load from file path
                json = await File.ReadAllTextAsync(url);
            }
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            };

            var manifest = JsonSerializer.Deserialize<CheckManifest>(json, options);
            if (manifest != null)
            {
                return manifest;
            }
        }
        catch
        {
            // Fall through to embedded/default
        }

        // If URL/file loading failed, try embedded resource
        var embeddedManifest = LoadEmbeddedManifest();
        if (embeddedManifest != null)
        {
            return embeddedManifest;
        }

        // Last resort: code-based default
        return GetDefaultManifest();
    }

    private CheckManifest? LoadEmbeddedManifest()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Maui.Cli.default-manifest.json";
            
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                // Fallback to code-based default if resource not found
                return GetDefaultManifest();
            }
            
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            };

            return JsonSerializer.Deserialize<CheckManifest>(json, options) ?? GetDefaultManifest();
        }
        catch
        {
            // Ultimate fallback
            return GetDefaultManifest();
        }
    }

    public CheckManifest GetDefaultManifest()
    {
        // Default manifest based on latest .NET MAUI requirements (2024/2025)
        return new CheckManifest
        {
            Check = new CheckConfiguration
            {
                ToolVersion = "1.0.0",
                Variables = new Dictionary<string, string>
                {
                    ["DOTNET_SDK_VERSION"] = "8.0.0",
                    ["OPENJDK_VERSION"] = "17.0",
                    ["MIN_ANDROID_API"] = "21",
                    ["TARGET_ANDROID_API"] = "34"
                },
                OpenJdk = new OpenJdkConfiguration
                {
                    Version = "17.0"
                },
                Xcode = new XcodeConfiguration
                {
                    MinimumVersion = "15",
                    MinimumVersionName = "15.0",
                    ExactVersion = null, // Allow any 15+
                    ExactVersionName = null
                },
                Android = new AndroidConfiguration
                {
                    Packages = new List<AndroidPackage>
                    {
                        new() { Path = "platforms;android-34", Version = "1" },
                        new() { Path = "platforms;android-33", Version = "1" },
                        new() { Path = "build-tools;34.0.0", Version = "34.0.0" },
                        new() { Path = "platform-tools", Version = "34.0.0" }
                    }
                },
                DotNet = new DotNetConfiguration
                {
                    Sdks = new List<DotNetSdk>
                    {
                        new()
                        {
                            Version = "8.0.0",
                            RequireExact = false,
                            WorkloadIds = new List<string> { "maui", "android", "ios", "maccatalyst", "macos" }
                        }
                    }
                },
                VisualStudio = new VisualStudioConfiguration
                {
                    MinimumVersion = "17.8"
                }
            }
        };
    }
}
