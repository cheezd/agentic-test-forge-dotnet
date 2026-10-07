using AgenticTestForge.Cli;

namespace AgenticTestForge.Tests;

[Trait("Category", "Unit")]
public class ForgeArgsTests
{
    [Fact]
    public void EmptyArgsRequestHelp()
    {
        var parsed = ForgeArgs.Parse([]);

        Assert.True(parsed.Help);
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    public void HelpFlagRequestsHelp(string flag)
    {
        var parsed = ForgeArgs.Parse(["check", flag]);

        Assert.True(parsed.Help);
    }

    [Fact]
    public void CheckParsesSharedFlags()
    {
        var parsed = ForgeArgs.Parse([
            "check",
            "--base",
            "main",
            "--path",
            "src/App",
            "--path",
            "src/Other",
            "--json",
            "report.json",
        ]);

        Assert.False(parsed.Help);
        Assert.Null(parsed.Error);
        Assert.Equal(ForgeVerbs.Check, parsed.Verb);
        Assert.Equal("main", parsed.BaseRef);
        Assert.Equal(["src/App", "src/Other"], parsed.Paths);
        Assert.Equal("report.json", parsed.JsonPath);
        Assert.Null(parsed.Threshold);
    }

    [Theory]
    [InlineData("crap", "30")]
    [InlineData("mutate", "80")]
    [InlineData("mutate-gherkin", "80")]
    public void ThresholdVerbsAcceptTheOverride(string verb, string raw)
    {
        var parsed = ForgeArgs.Parse([verb, "--threshold", raw]);

        Assert.Null(parsed.Error);
        Assert.Equal(
            double.Parse(raw, System.Globalization.CultureInfo.InvariantCulture),
            parsed.Threshold
        );
    }

    [Theory]
    [InlineData("check")]
    [InlineData("dry")]
    public void ThresholdOnOtherVerbsIsAToolError(string verb)
    {
        var parsed = ForgeArgs.Parse([verb, "--threshold", "10"]);

        Assert.NotNull(parsed.Error);
        Assert.Contains("--threshold", parsed.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void EqualsFormSetsTheOptionValue()
    {
        var parsed = ForgeArgs.Parse(["mutate", "--base=main", "--threshold=80"]);

        Assert.Null(parsed.Error);
        Assert.Equal("main", parsed.BaseRef);
        Assert.Equal(80, parsed.Threshold);
    }

    [Fact]
    public void UnknownOptionIsAToolError()
    {
        var parsed = ForgeArgs.Parse(["check", "--full"]);

        Assert.Contains("Unknown option", parsed.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void MissingOptionValueIsAToolError()
    {
        var parsed = ForgeArgs.Parse(["check", "--json"]);

        Assert.Contains("requires a value", parsed.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void UnexpectedArgumentIsAToolError()
    {
        var parsed = ForgeArgs.Parse(["check", "src"]);

        Assert.Contains("Unexpected argument", parsed.Error, StringComparison.Ordinal);
    }
}
