using System.IO.Compression;
using System.Net.Http.Json;
using System.Text.Json;
using Maui.Cli.Models;
using Spectre.Console;

namespace Maui.Cli.Services;

internal sealed class GitHubArtifactService : IGitHubArtifactService
{
    private readonly HttpClient _httpClient;
    private readonly IAnsiConsole _console;
    private const string MauiRepoOwner = "dotnet";
    private const string MauiRepoName = "maui";

    public GitHubArtifactService(HttpClient httpClient, IAnsiConsole console)
    {
        _httpClient = httpClient;
        _console = console;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Maui-CLI/1.0");
        
        // Check for GitHub token in environment variable
        var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        if (!string.IsNullOrEmpty(githubToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", githubToken);
        }
    }

    public async Task<IReadOnlyList<PRArtifactInfo>> GetPRArtifactsAsync(int prNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get PR details to find associated workflow runs
            var prUrl = $"https://api.github.com/repos/{MauiRepoOwner}/{MauiRepoName}/pulls/{prNumber}";
            var prResponse = await _httpClient.GetAsync(prUrl, cancellationToken);
            
            if (!prResponse.IsSuccessStatusCode)
            {
                _console.MarkupLine($"[red]Failed to fetch PR {prNumber}: {prResponse.StatusCode}[/]");
                return Array.Empty<PRArtifactInfo>();
            }

            var prJson = await prResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            var headSha = prJson.GetProperty("head").GetProperty("sha").GetString();
            
            if (string.IsNullOrEmpty(headSha))
            {
                _console.MarkupLine("[red]Could not determine HEAD SHA for PR[/]");
                return Array.Empty<PRArtifactInfo>();
            }

            // Get workflow runs for this SHA
            var runsUrl = $"https://api.github.com/repos/{MauiRepoOwner}/{MauiRepoName}/actions/runs?head_sha={headSha}&per_page=10";
            var runsResponse = await _httpClient.GetAsync(runsUrl, cancellationToken);
            
            if (!runsResponse.IsSuccessStatusCode)
            {
                _console.MarkupLine($"[red]Failed to fetch workflow runs: {runsResponse.StatusCode}[/]");
                return Array.Empty<PRArtifactInfo>();
            }

            var runsJson = await runsResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            var workflowRuns = runsJson.GetProperty("workflow_runs").EnumerateArray().ToList();

            var artifacts = new List<PRArtifactInfo>();

            // Look for artifacts in the workflow runs
            foreach (var run in workflowRuns)
            {
                var runId = run.GetProperty("id").GetInt64();
                var conclusion = run.TryGetProperty("conclusion", out var c) ? c.GetString() : null;
                
                // Only look at successful runs
                if (conclusion != "success") continue;

                var artifactsUrl = $"https://api.github.com/repos/{MauiRepoOwner}/{MauiRepoName}/actions/runs/{runId}/artifacts";
                var artifactsResponse = await _httpClient.GetAsync(artifactsUrl, cancellationToken);
                
                if (!artifactsResponse.IsSuccessStatusCode) continue;

                var artifactsJson = await artifactsResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
                var artifactsList = artifactsJson.GetProperty("artifacts").EnumerateArray();

                foreach (var artifact in artifactsList)
                {
                    var name = artifact.GetProperty("name").GetString();
                    
                    // Look for NuGet package artifacts
                    if (name?.Contains("nuget", StringComparison.OrdinalIgnoreCase) == true ||
                        name?.Contains("nupkg", StringComparison.OrdinalIgnoreCase) == true ||
                        name?.Contains("packages", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        artifacts.Add(new PRArtifactInfo
                        {
                            PullRequestNumber = prNumber,
                            BuildId = runId.ToString(),
                            ArtifactName = name ?? "unknown",
                            DownloadUrl = artifact.GetProperty("archive_download_url").GetString() ?? "",
                            SizeInBytes = artifact.GetProperty("size_in_bytes").GetInt64()
                        });
                    }
                }
            }

            return artifacts;
        }
        catch (Exception ex)
        {
            _console.MarkupLine($"[red]Error fetching PR artifacts: {ex.Message}[/]");
            return Array.Empty<PRArtifactInfo>();
        }
    }

    public async Task<string> DownloadArtifactAsync(
        PRArtifactInfo artifact, 
        DirectoryInfo targetDirectory, 
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!targetDirectory.Exists)
            {
                targetDirectory.Create();
            }

            var zipFilePath = Path.Combine(targetDirectory.FullName, $"{artifact.ArtifactName}.zip");
            
            // Download the artifact
            var response = await _httpClient.GetAsync(artifact.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || 
                response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _console.MarkupLine("[red]Authentication required to download artifacts.[/]");
                _console.MarkupLine("[yellow]Please set GITHUB_TOKEN environment variable with a valid GitHub personal access token.[/]");
                _console.MarkupLine("[yellow]Create a token at: https://github.com/settings/tokens[/]");
                throw new UnauthorizedAccessException("GitHub authentication required");
            }
            
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? 0;
            var downloadedBytes = 0L;

            using (var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken))
            using (var fileStream = File.Create(zipFilePath))
            {
                var buffer = new byte[8192];
                int bytesRead;
                
                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                    downloadedBytes += bytesRead;
                    
                    if (totalBytes > 0)
                    {
                        progress?.Report((double)downloadedBytes / totalBytes * 100.0);
                    }
                }
            }

            // Extract the zip file
            var extractPath = Path.Combine(targetDirectory.FullName, artifact.ArtifactName);
            if (Directory.Exists(extractPath))
            {
                Directory.Delete(extractPath, true);
            }
            
            ZipFile.ExtractToDirectory(zipFilePath, extractPath);
            File.Delete(zipFilePath);

            return extractPath;
        }
        catch (Exception ex)
        {
            _console.MarkupLine($"[red]Error downloading artifact: {ex.Message}[/]");
            throw;
        }
    }
}
