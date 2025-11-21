using FluentAssertions;
using Maui.Cli.Commands;
using Maui.Cli.Models;
using Maui.Cli.Services;
using Moq;

namespace Maui.Cli.Tests;

public class UpgradeCommandTests
{
    private readonly Mock<IMauiProjectUpdater> _mockProjectUpdater;
    private readonly Mock<IProjectLocator> _mockProjectLocator;
    private readonly UpgradeCommand _command;

    public UpgradeCommandTests()
    {
        _mockProjectUpdater = new Mock<IMauiProjectUpdater>();
        _mockProjectLocator = new Mock<IProjectLocator>();
        _command = new UpgradeCommand(_mockProjectUpdater.Object, _mockProjectLocator.Object);
    }

    [Fact]
    public void Constructor_Should_CreateCommandWithCorrectName()
    {
        // Assert
        _command.Name.Should().Be("upgrade");
    }

    [Fact]
    public void Constructor_Should_CreateCommandWithDescription()
    {
        // Assert
        _command.Description.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Command_Should_HaveProjectOption()
    {
        // Assert
        _command.Options.Should().Contain(o => o.Name == "project");
    }

    [Fact]
    public void Command_Should_HaveChannelOption()
    {
        // Assert
        _command.Options.Should().Contain(o => o.Name == "channel");
    }
}

public class MauiChannelTests
{
    [Fact]
    public void CreateNet9StableChannel_Should_ReturnNet9StableChannel()
    {
        // Act
        var channel = MauiChannel.CreateNet9StableChannel();

        // Assert
        channel.Name.Should().Be("net9-stable");
        channel.Type.Should().Be(MauiChannelType.Stable);
        channel.FeedUrl.Should().Be("https://api.nuget.org/v3/index.json");
        channel.TargetFramework.Should().Be("net9.0");
    }

    [Fact]
    public void CreateNet10StableChannel_Should_ReturnNet10StableChannel()
    {
        // Act
        var channel = MauiChannel.CreateNet10StableChannel();

        // Assert
        channel.Name.Should().Be("net10-stable");
        channel.Type.Should().Be(MauiChannelType.Stable);
        channel.FeedUrl.Should().Be("https://api.nuget.org/v3/index.json");
        channel.TargetFramework.Should().Be("net10.0");
    }

    [Fact]
    public void CreateNet9NightlyChannel_Should_ReturnNet9Channel()
    {
        // Act
        var channel = MauiChannel.CreateNet9NightlyChannel();

        // Assert
        channel.Name.Should().Be("net9-nightly");
        channel.Type.Should().Be(MauiChannelType.Nightly);
        channel.TargetFramework.Should().Be("net9.0");
        channel.FeedUrl.Should().Contain("dotnet9");
    }

    [Fact]
    public void CreateNet10NightlyChannel_Should_ReturnNet10Channel()
    {
        // Act
        var channel = MauiChannel.CreateNet10NightlyChannel();

        // Assert
        channel.Name.Should().Be("net10-nightly");
        channel.Type.Should().Be(MauiChannelType.Nightly);
        channel.TargetFramework.Should().Be("net10.0");
        channel.FeedUrl.Should().Contain("dotnet10");
    }
}

public class ProjectLocatorTests
{
    private readonly ProjectLocator _locator;
    private readonly string _tempDirectory;

    public ProjectLocatorTests()
    {
        _locator = new ProjectLocator();
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task FindProjectFileAsync_Should_ReturnNull_WhenDirectoryDoesNotExist()
    {
        // Arrange
        var nonExistentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act
        var result = await _locator.FindProjectFileAsync(nonExistentDir);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindProjectFileAsync_Should_ReturnNull_WhenNoProjectFilesExist()
    {
        // Act
        var result = await _locator.FindProjectFileAsync(_tempDirectory);

        // Assert
        result.Should().BeNull();

        // Cleanup
        Directory.Delete(_tempDirectory, true);
    }

    [Fact]
    public async Task FindProjectFileAsync_Should_ReturnProjectFile_WhenOneExists()
    {
        // Arrange
        var projectPath = Path.Combine(_tempDirectory, "Test.csproj");
        File.WriteAllText(projectPath, "<Project></Project>");

        // Act
        var result = await _locator.FindProjectFileAsync(_tempDirectory);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test.csproj");

        // Cleanup
        Directory.Delete(_tempDirectory, true);
    }

    [Fact]
    public async Task FindProjectFileAsync_Should_PreferMauiProject_WhenMultipleExist()
    {
        // Arrange
        var project1Path = Path.Combine(_tempDirectory, "Test.csproj");
        var project2Path = Path.Combine(_tempDirectory, "MyMauiApp.csproj");
        File.WriteAllText(project1Path, "<Project></Project>");
        File.WriteAllText(project2Path, "<Project></Project>");

        // Act
        var result = await _locator.FindProjectFileAsync(_tempDirectory);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("MyMauiApp.csproj");

        // Cleanup
        Directory.Delete(_tempDirectory, true);
    }
}

public class ProcessRunnerTests
{
    private readonly ProcessRunner _runner;

    public ProcessRunnerTests()
    {
        _runner = new ProcessRunner();
    }

    [Fact]
    public async Task RunProcessAsync_Should_ReturnExitCode()
    {
        // Act
        var result = await _runner.RunProcessAsync("dotnet", "--version", Environment.CurrentDirectory);

        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RunProcessAsync_Should_CaptureOutput()
    {
        // Act
        var result = await _runner.RunProcessAsync("dotnet", "--version", Environment.CurrentDirectory);

        // Assert
        result.Output.Should().NotBeNullOrEmpty();
        result.Output.Should().MatchRegex(@"\d+\.\d+\.\d+");
    }

    [Fact]
    public async Task RunProcessAsync_Should_HandleInvalidCommand()
    {
        // Act
        var act = async () => await _runner.RunProcessAsync("nonexistentcommand", "", Environment.CurrentDirectory);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }
}
