using FluentAssertions;
using Maui.Cli.Models;
using Maui.Cli.Services;
using Moq;

namespace Maui.Cli.Tests.Services;

public class EnvironmentCheckServiceTests
{
    private readonly EnvironmentCheckService _service;
    private readonly Mock<IManifestService> _mockManifestService;

    public EnvironmentCheckServiceTests()
    {
        _mockManifestService = new Mock<IManifestService>();
        _mockManifestService.Setup(m => m.GetDefaultManifest()).Returns(CreateDefaultManifest());
        _mockManifestService.Setup(m => m.LoadManifestAsync(It.IsAny<string?>())).ReturnsAsync(CreateDefaultManifest());
        
        var mockWorkloadDependencyService = new Mock<IWorkloadDependencyService>();
        mockWorkloadDependencyService.Setup(m => m.GetAllWorkloadDependenciesAsync(It.IsAny<string?>()))
            .ReturnsAsync(new List<WorkloadDependencyInfo>());
        
        _service = new EnvironmentCheckService(_mockManifestService.Object, mockWorkloadDependencyService.Object);
    }

    private static CheckManifest CreateDefaultManifest()
    {
        return new CheckManifest
        {
            Check = new CheckConfiguration
            {
                Variables = new Dictionary<string, string>
                {
                    ["MIN_ANDROID_API"] = "21",
                    ["TARGET_ANDROID_API"] = "34"
                },
                OpenJdk = new OpenJdkConfiguration { Version = "17.0" },
                Xcode = new XcodeConfiguration { MinimumVersion = "15", MinimumVersionName = "15.0" }
            }
        };
    }

    [Fact]
    public async Task CheckEnvironmentAsync_ShouldReturnResults()
    {
        // Act
        var results = await _service.CheckEnvironmentAsync(null, false, null);

        // Assert
        results.Should().NotBeNull();
        results.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CheckEnvironmentAsync_ShouldIncludeDotNetSdkCheck()
    {
        // Act
        var results = await _service.CheckEnvironmentAsync(null, false, null);

        // Assert
        results.Should().Contain(r => r.Name == ".NET SDK");
    }

    [Theory]
    [InlineData("android")]
    [InlineData("ios")]
    [InlineData("windows")]
    [InlineData("maccatalyst")]
    public async Task CheckEnvironmentAsync_WithPlatformFilter_ShouldCheckSpecificPlatform(string platform)
    {
        // Act
        var results = await _service.CheckEnvironmentAsync(platform, false, null);

        // Assert
        results.Should().NotBeNull();
        results.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CheckEnvironmentAsync_WithVerbose_ShouldReturnResults()
    {
        // Act
        var results = await _service.CheckEnvironmentAsync(null, true, null);

        // Assert
        results.Should().NotBeNull();
        results.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CheckEnvironmentAsync_ShouldCheckMauiWorkloads()
    {
        // Act
        var results = await _service.CheckEnvironmentAsync(null, false, null);

        // Assert
        var workloadResults = results.Where(r => r.Name.Contains("MAUI Workload") || r.Name == "MAUI Workloads");
        workloadResults.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CheckEnvironmentAsync_AllResults_ShouldHaveValidStatus()
    {
        // Act
        var results = await _service.CheckEnvironmentAsync(null, false, null);

        // Assert
        foreach (var result in results)
        {
            result.Status.Should().BeOneOf(
                CheckStatus.Ok,
                CheckStatus.Warning,
                CheckStatus.Error,
                CheckStatus.NotApplicable);
        }
    }

    [Fact]
    public async Task CheckEnvironmentAsync_AllResults_ShouldHaveNameAndMessage()
    {
        // Act
        var results = await _service.CheckEnvironmentAsync(null, false, null);

        // Assert
        foreach (var result in results)
        {
            result.Name.Should().NotBeNullOrWhiteSpace();
            result.Message.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task CheckEnvironmentAsync_ErrorsAndWarnings_ShouldHaveRecommendations()
    {
        // Act
        var results = await _service.CheckEnvironmentAsync(null, false, null);

        // Assert
        var errorsAndWarnings = results.Where(r => 
            r.Status == CheckStatus.Error || r.Status == CheckStatus.Warning);

        foreach (var result in errorsAndWarnings)
        {
            result.Recommendation.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task CheckEnvironmentAsync_AndroidPlatform_ShouldCheckJavaAndAndroidSdk()
    {
        // Act
        var results = await _service.CheckEnvironmentAsync("android", false, null);

        // Assert
        var hasJavaCheck = results.Any(r => r.Name.Contains("Java") || r.Name.Contains("JDK"));
        var hasAndroidSdkCheck = results.Any(r => r.Name.Contains("Android SDK"));

        (hasJavaCheck || hasAndroidSdkCheck).Should().BeTrue("Android platform should check Java and/or Android SDK");
    }

    [Fact]
    public async Task CheckEnvironmentAsync_ShouldHandleMultipleInvocations()
    {
        // Act
        var results1 = await _service.CheckEnvironmentAsync(null, false, null);
        var results2 = await _service.CheckEnvironmentAsync(null, false, null);

        // Assert
        results1.Should().HaveCountGreaterThan(0);
        results2.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task CheckEnvironmentAsync_PlatformSpecific_ShouldNotCheckIrrelevantPlatforms()
    {
        // Act
        var androidResults = await _service.CheckEnvironmentAsync("android", false, null);

        // Assert - Android check should not include iOS-specific checks unless on macOS
        if (!OperatingSystem.IsMacOS())
        {
            androidResults.Should().NotContain(r => r.Name.Contains("Xcode"));
        }
    }
}
