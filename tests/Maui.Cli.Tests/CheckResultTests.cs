using FluentAssertions;
using Maui.Cli.Models;

namespace Maui.Cli.Tests.Models;

public class CheckResultTests
{
    [Fact]
    public void CheckResult_ShouldBeCreatable_WithRequiredProperties()
    {
        // Act
        var result = new CheckResult
        {
            Name = "Test Component",
            Status = CheckStatus.Ok,
            Message = "Test message"
        };

        // Assert
        result.Name.Should().Be("Test Component");
        result.Status.Should().Be(CheckStatus.Ok);
        result.Message.Should().Be("Test message");
    }

    [Fact]
    public void CheckResult_ShouldAllowOptionalProperties()
    {
        // Act
        var result = new CheckResult
        {
            Name = "Test Component",
            Status = CheckStatus.Warning,
            Message = "Test message",
            Recommendation = "Do this to fix",
            Details = new Dictionary<string, string> { ["key"] = "value" }
        };

        // Assert
        result.Recommendation.Should().Be("Do this to fix");
        result.Details.Should().ContainKey("key");
        result.Details!["key"].Should().Be("value");
    }

    [Theory]
    [InlineData(CheckStatus.Ok)]
    [InlineData(CheckStatus.Warning)]
    [InlineData(CheckStatus.Error)]
    [InlineData(CheckStatus.NotApplicable)]
    public void CheckResult_ShouldSupportAllStatusValues(CheckStatus status)
    {
        // Act
        var result = new CheckResult
        {
            Name = "Test",
            Status = status,
            Message = "Test"
        };

        // Assert
        result.Status.Should().Be(status);
    }

    [Fact]
    public void CheckStatus_ShouldHaveExpectedValues()
    {
        // Assert
        Enum.GetValues<CheckStatus>().Should().Contain(new[]
        {
            CheckStatus.Ok,
            CheckStatus.Warning,
            CheckStatus.Error,
            CheckStatus.NotApplicable
        });
    }
}
