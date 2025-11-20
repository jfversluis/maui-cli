using FluentAssertions;
using Maui.Cli.Services;
using Moq;
using Moq.Protected;
using Spectre.Console.Testing;
using System.Net;
using System.Text.Json;

namespace Maui.Cli.Tests;

public class GitHubArtifactServiceTests
{
    [Fact]
    public async Task GetPRArtifactsAsync_ReturnsEmpty_WhenPRNotFound()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var console = new TestConsole();
        var service = new GitHubArtifactService(httpClient, console);

        // Act
        var artifacts = await service.GetPRArtifactsAsync(99999);

        // Assert
        artifacts.Should().BeEmpty();
        console.Output.Should().Contain("Failed to fetch PR");
    }

    [Fact]
    public async Task GetPRArtifactsAsync_ReturnsEmpty_WhenNoSuccessfulRuns()
    {
        // Arrange
        var prResponse = new
        {
            head = new { sha = "abc123" }
        };

        var runsResponse = new
        {
            workflow_runs = new[]
            {
                new
                {
                    id = 123456,
                    conclusion = "failure"
                }
            }
        };

        var handlerMock = new Mock<HttpMessageHandler>();
        var callCount = 0;
        
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1) // PR call
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(JsonSerializer.Serialize(prResponse))
                    };
                }
                else // Runs call
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(JsonSerializer.Serialize(runsResponse))
                    };
                }
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var console = new TestConsole();
        var service = new GitHubArtifactService(httpClient, console);

        // Act
        var artifacts = await service.GetPRArtifactsAsync(12345);

        // Assert
        artifacts.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_SetsAuthorizationHeader_WhenGitHubTokenIsSet()
    {
        // Arrange
        var token = "test_token_12345";
        Environment.SetEnvironmentVariable("GITHUB_TOKEN", token);

        try
        {
            var httpClient = new HttpClient();
            var console = new TestConsole();

            // Act
            var service = new GitHubArtifactService(httpClient, console);

            // Assert
            httpClient.DefaultRequestHeaders.Authorization.Should().NotBeNull();
            httpClient.DefaultRequestHeaders.Authorization!.Scheme.Should().Be("Bearer");
            httpClient.DefaultRequestHeaders.Authorization!.Parameter.Should().Be(token);
        }
        finally
        {
            Environment.SetEnvironmentVariable("GITHUB_TOKEN", null);
        }
    }
}
