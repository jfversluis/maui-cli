using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace Maui.Cli.Tests;

public class RegexTest
{
    private readonly ITestOutputHelper _output;

    public RegexTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData("net9.0-tizen", "net10.0-tizen")]
    [InlineData("net9.0-android", "net10.0-android")]
    [InlineData("net9.0-ios", "net10.0-ios")]
    [InlineData("net9.0-windows10.0.19041.0", "net10.0-windows10.0.19041.0")]
    [InlineData("<!-- net9.0-tizen -->", "<!-- net10.0-tizen -->")]
    [InlineData("<!-- <TargetFrameworks>$(TargetFrameworks);net9.0-tizen</TargetFrameworks> -->", "<!-- <TargetFrameworks>$(TargetFrameworks);net10.0-tizen</TargetFrameworks> -->")]
    public void TfmRegex_ShouldReplaceCorrectly(string input, string expected)
    {
        var oldTfm = "net9.0";
        var newTfm = "net10.0";
        var pattern = $@"{Regex.Escape(oldTfm)}(?=\b|-)";
        
        _output.WriteLine($"Pattern: {pattern}");
        _output.WriteLine($"Input: {input}");
        
        var result = Regex.Replace(input, pattern, newTfm);
        
        _output.WriteLine($"Result: {result}");
        _output.WriteLine($"Expected: {expected}");
        
        Assert.Equal(expected, result);
    }
}
