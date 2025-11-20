namespace Maui.Cli.Models;

internal sealed class CheckResult
{
    public required string Name { get; init; }
    public required CheckStatus Status { get; init; }
    public string? Message { get; init; }
    public string? Recommendation { get; init; }
    public Dictionary<string, string>? Details { get; init; }
}

public enum CheckStatus
{
    Ok,
    Warning,
    Error,
    NotApplicable
}
