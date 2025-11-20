using System.CommandLine;
using FluentAssertions;
using Maui.Cli.Commands;
using Maui.Cli.Models;
using Maui.Cli.Services;
using Moq;
using Spectre.Console;
using Spectre.Console.Testing;

namespace Maui.Cli.Tests.Commands;

public class CheckCommandTests
{
    private readonly Mock<IEnvironmentCheckService> _mockCheckService;
    private readonly TestConsole _testConsole;
    private readonly CheckCommand _command;

    public CheckCommandTests()
    {
        _mockCheckService = new Mock<IEnvironmentCheckService>();
        _testConsole = new TestConsole();
        _command = new CheckCommand(_mockCheckService.Object, _testConsole);
    }

    [Fact]
    public void Constructor_ShouldCreateCommand_WithCorrectName()
    {
        // Assert
        _command.Name.Should().Be("check");
    }

    [Fact]
    public void Constructor_ShouldCreateCommand_WithDescription()
    {
        // Assert
        _command.Description.Should().Contain("environment");
    }

    [Fact]
    public async Task ExecuteAsync_WithAllOk_ShouldReturnSuccess()
    {
        // Arrange
        var okResults = new List<CheckResult>
        {
            new CheckResult
            {
                Name = ".NET SDK",
                Status = CheckStatus.Ok,
                Message = "Version 9.0.100"
            },
            new CheckResult
            {
                Name = "MAUI Workload",
                Status = CheckStatus.Ok,
                Message = "Installed"
            }
        };

        _mockCheckService
            .Setup(s => s.CheckEnvironmentAsync(null, false, It.IsAny<string?>()))
            .ReturnsAsync(okResults);

        // Act
        var exitCode = await _command.InvokeAsync(string.Empty);

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_WithErrors_ShouldReturnError()
    {
        // Arrange
        var errorResults = new List<CheckResult>
        {
            new CheckResult
            {
                Name = ".NET SDK",
                Status = CheckStatus.Error,
                Message = "Not found",
                Recommendation = "Install from https://dot.net"
            }
        };

        _mockCheckService
            .Setup(s => s.CheckEnvironmentAsync(null, false, It.IsAny<string?>()))
            .ReturnsAsync(errorResults);

        // Act
        var exitCode = await _command.InvokeAsync(string.Empty);

        // Assert
        exitCode.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_WithWarningsOnly_ShouldReturnSuccess()
    {
        // Arrange
        var warningResults = new List<CheckResult>
        {
            new CheckResult
            {
                Name = ".NET SDK",
                Status = CheckStatus.Warning,
                Message = "Version 7.0 detected",
                Recommendation = "Update to .NET 8+"
            }
        };

        _mockCheckService
            .Setup(s => s.CheckEnvironmentAsync(null, false, It.IsAny<string?>()))
            .ReturnsAsync(warningResults);

        // Act
        var exitCode = await _command.InvokeAsync(string.Empty);

        // Assert
        exitCode.Should().Be(0, "Warnings should not cause failure");
    }

    [Fact]
    public async Task ExecuteAsync_WithVerboseFlag_ShouldPassVerboseToService()
    {
        // Arrange
        var results = new List<CheckResult>
        {
            new CheckResult
            {
                Name = "Test",
                Status = CheckStatus.Ok,
                Message = "OK"
            }
        };

        _mockCheckService
            .Setup(s => s.CheckEnvironmentAsync(null, true, It.IsAny<string?>()))
            .ReturnsAsync(results);

        // Act
        await _command.InvokeAsync("--verbose");

        // Assert
        _mockCheckService.Verify(s => s.CheckEnvironmentAsync(null, true, It.IsAny<string?>()), Times.Once);
    }

    [Theory]
    [InlineData("--platform android", "android")]
    [InlineData("-p ios", "ios")]
    [InlineData("--platform windows", "windows")]
    public async Task ExecuteAsync_WithPlatformFlag_ShouldPassPlatformToService(string args, string expectedPlatform)
    {
        // Arrange
        var results = new List<CheckResult>
        {
            new CheckResult
            {
                Name = "Test",
                Status = CheckStatus.Ok,
                Message = "OK"
            }
        };

        _mockCheckService
            .Setup(s => s.CheckEnvironmentAsync(expectedPlatform, false, It.IsAny<string?>()))
            .ReturnsAsync(results);

        // Act
        await _command.InvokeAsync(args);

        // Assert
        _mockCheckService.Verify(s => s.CheckEnvironmentAsync(expectedPlatform, false, It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldDisplayResults_InTable()
    {
        // Arrange
        var results = new List<CheckResult>
        {
            new CheckResult
            {
                Name = ".NET SDK",
                Status = CheckStatus.Ok,
                Message = "Version 9.0.100"
            }
        };

        _mockCheckService
            .Setup(s => s.CheckEnvironmentAsync(null, false, It.IsAny<string?>()))
            .ReturnsAsync(results);

        // Act
        await _command.InvokeAsync(string.Empty);

        // Assert
        var output = _testConsole.Output;
        output.Should().Contain(".NET SDK");
        output.Should().Contain("Version 9.0.100");
    }

    [Fact]
    public async Task ExecuteAsync_WithErrors_ShouldDisplayRecommendations()
    {
        // Arrange
        var results = new List<CheckResult>
        {
            new CheckResult
            {
                Name = ".NET SDK",
                Status = CheckStatus.Error,
                Message = "Not found",
                Recommendation = "Install from https://dot.net"
            }
        };

        _mockCheckService
            .Setup(s => s.CheckEnvironmentAsync(null, false, It.IsAny<string?>()))
            .ReturnsAsync(results);

        // Act
        await _command.InvokeAsync(string.Empty);

        // Assert
        var output = _testConsole.Output;
        output.Should().Contain("Install from https://dot.net");
    }

    [Fact]
    public async Task ExecuteAsync_WithMixedResults_ShouldDisplayBothOkAndErrors()
    {
        // Arrange
        var results = new List<CheckResult>
        {
            new CheckResult
            {
                Name = ".NET SDK",
                Status = CheckStatus.Ok,
                Message = "Version 9.0.100"
            },
            new CheckResult
            {
                Name = "Java JDK",
                Status = CheckStatus.Error,
                Message = "Not found",
                Recommendation = "Install JDK"
            }
        };

        _mockCheckService
            .Setup(s => s.CheckEnvironmentAsync(null, false, It.IsAny<string?>()))
            .ReturnsAsync(results);

        // Act
        var exitCode = await _command.InvokeAsync(string.Empty);

        // Assert
        exitCode.Should().Be(1, "Should return error when any check fails");
        var output = _testConsole.Output;
        output.Should().Contain(".NET SDK");
        output.Should().Contain("Java JDK");
        output.Should().Contain("Install JDK");
    }

    [Fact]
    public async Task ExecuteAsync_WithNotApplicable_ShouldHandleGracefully()
    {
        // Arrange
        var results = new List<CheckResult>
        {
            new CheckResult
            {
                Name = "Xcode",
                Status = CheckStatus.NotApplicable,
                Message = "Not applicable on this platform"
            }
        };

        _mockCheckService
            .Setup(s => s.CheckEnvironmentAsync(null, false, It.IsAny<string?>()))
            .ReturnsAsync(results);

        // Act
        var exitCode = await _command.InvokeAsync(string.Empty);

        // Assert
        exitCode.Should().Be(0);
        var output = _testConsole.Output;
        output.Should().Contain("Xcode");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCallServiceExactlyOnce()
    {
        // Arrange
        var results = new List<CheckResult>
        {
            new CheckResult { Name = "Test", Status = CheckStatus.Ok, Message = "OK" }
        };

        _mockCheckService
            .Setup(s => s.CheckEnvironmentAsync(It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<string?>()))
            .ReturnsAsync(results);

        // Act
        await _command.InvokeAsync(string.Empty);

        // Assert
        _mockCheckService.Verify(
            s => s.CheckEnvironmentAsync(It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<string?>()),
            Times.Once);
    }
}
