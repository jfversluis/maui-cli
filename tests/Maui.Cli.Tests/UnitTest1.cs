using FluentAssertions;
using Maui.Cli.Models;
using Maui.Cli.Services;
using Moq;
using Spectre.Console;
using Spectre.Console.Testing;

namespace Maui.Cli.Tests;

public class NuGetServiceTests
{
    [Fact]
    public async Task GetMauiPackagesAsync_ReturnsEmptyList_WhenDirectoryDoesNotExist()
    {
        // Arrange
        var console = new TestConsole();
        var service = new NuGetService(console);
        var nonExistentDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

        // Act
        var packages = await service.GetMauiPackagesAsync(nonExistentDir);

        // Assert
        packages.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMauiPackagesAsync_ReturnsPackages_WhenDirectoryContainsMauiPackages()
    {
        // Arrange
        var console = new TestConsole();
        var service = new NuGetService(console);
        var tempDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
        
        try
        {
            // Create mock package files
            File.WriteAllText(Path.Combine(tempDir.FullName, "Microsoft.Maui.Core.9.0.0.nupkg"), "test");
            File.WriteAllText(Path.Combine(tempDir.FullName, "Microsoft.Maui.Controls.9.0.0.nupkg"), "test");
            File.WriteAllText(Path.Combine(tempDir.FullName, "SomeOther.Package.1.0.0.nupkg"), "test");

            // Act
            var packages = await service.GetMauiPackagesAsync(tempDir);

            // Assert
            packages.Should().HaveCount(2);
            packages.Should().Contain(p => p.Contains("Microsoft.Maui.Core"));
            packages.Should().Contain(p => p.Contains("Microsoft.Maui.Controls"));
        }
        finally
        {
            tempDir.Delete(true);
        }
    }
}

public class CliExecutionContextTests
{
    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var workingDir = new DirectoryInfo(Path.GetTempPath());
        var hivesDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "hives"));
        var cacheDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "cache"));

        // Act
        var context = new CliExecutionContext(workingDir, hivesDir, cacheDir, debugMode: true);

        // Assert
        context.WorkingDirectory.Should().Be(workingDir);
        context.HivesDirectory.Should().Be(hivesDir);
        context.CacheDirectory.Should().Be(cacheDir);
        context.DebugMode.Should().BeTrue();
    }
}
