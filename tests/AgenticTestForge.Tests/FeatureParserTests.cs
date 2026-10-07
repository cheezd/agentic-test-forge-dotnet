using AgenticTestForge.Gherkin;

namespace AgenticTestForge.Tests;

[Trait("Category", "Unit")]
public class FeatureParserTests
{
    [Fact]
    public void ParsesAnOutlineAndRewritesOneCell()
    {
        const string text = """
            Feature: Score

              Scenario Outline: Positive stays positive
                Given a value of <value>
                Then the value is positive
                Examples:
                  | value |
                  | 2     |
            """;

        var parsed = FeatureParser.Parse(text, "features/score.feature");
        var scenario = Assert.Single(parsed.Scenarios!);
        Assert.Equal("Positive stays positive", scenario.Name);
        Assert.Equal(["a value of <value>", "the value is positive"], scenario.Steps);
        var row = Assert.Single(scenario.Examples!.Rows);
        Assert.Equal(["2"], row.Cells);

        var mutated = FeatureParser.Apply(text, row, 0, "0");
        Assert.Contains("| 0 |", mutated, StringComparison.Ordinal);
        Assert.DoesNotContain("| 2     |", mutated, StringComparison.Ordinal);
    }
}
