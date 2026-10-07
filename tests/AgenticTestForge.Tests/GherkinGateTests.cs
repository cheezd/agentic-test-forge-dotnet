using AgenticTestForge.Cli;
using AgenticTestForge.Config;
using AgenticTestForge.Gherkin;
using AgenticTestForge.Reporting;

namespace AgenticTestForge.Tests;

[Trait("Category", "Unit")]
public class GherkinGateTests
{
    [Fact]
    public void AMissingStepIsInconclusiveAndDoesNotRunTests()
    {
        using var root = new TempDirectory();
        root.Write(
            "features/score.feature",
            """
            Feature: Score

              Scenario Outline: Positive stays positive
                Given a value of <value>
                Then the value is positive
                Examples:
                  | value |
                  | 2     |
            """
        );
        root.Write("steps/Missing.cs", """public class Missing { }""");
        var ran = false;

        var evaluation = GherkinGate.Evaluate(
            Plan(root.Path),
            (_, _, _) =>
            {
                ran = true;
                return 0;
            }
        );

        Assert.False(ran);
        Assert.Null(evaluation.Error);
        Assert.Equal(GateStatus.Fail, evaluation.Gate.Status);
        var report = Assert.IsType<GherkinGateReport>(evaluation.Gate);
        Assert.Contains(
            report.Inconclusive,
            issue => issue.Mutator.StartsWith("Undefined step:", StringComparison.Ordinal)
        );
    }

    [Fact]
    public void APassingExampleIsASurvivorAndTheRateUsesTheFloor()
    {
        using var root = new TempDirectory();
        root.Write(
            "features/score.feature",
            """
            Feature: Score

              Scenario Outline: Positive stays positive
                Given a value of <value>
                Then the value is positive
                Examples:
                  | value |
                  | 2     |
            """
        );
        root.Write(
            "steps/Steps.cs",
            """
            [Given("a value of {int}")]
            [Then("the value is positive")]
            public class Steps { }
            """
        );

        var feature = Path.Combine(root.Path, "features", "score.feature");
        var evaluation = GherkinGate.Evaluate(
            Plan(root.Path),
            (_, _, scenario) =>
            {
                if (scenario is null)
                {
                    return 0;
                }

                var text = File.ReadAllText(feature);
                return text.Contains("| 0 |", StringComparison.Ordinal) ? 1 : 0;
            }
        );

        Assert.Null(evaluation.Error);
        var report = Assert.IsType<GherkinGateReport>(evaluation.Gate);
        Assert.Equal(GateStatus.Fail, report.Status);
        var finding = Assert.Single(report.Findings);
        Assert.Equal(1, finding.Killed);
        Assert.Equal(3, finding.Total);
        Assert.True(finding.AboveThreshold);
        Assert.Equal(2, report.Survivors.Count);
        Assert.Contains(report.Survivors, survivor => survivor.Mutator == "2 -> 3");
    }

    [Fact]
    public void AnUnparsedFeatureIsAToolError()
    {
        using var root = new TempDirectory();
        root.Write("notes.feature", "this is not a feature");
        var ran = false;

        var evaluation = GherkinGate.Evaluate(
            Plan(root.Path),
            (_, _, _) =>
            {
                ran = true;
                return 0;
            }
        );

        Assert.False(ran);
        Assert.Contains("could not be parsed", evaluation.Error, StringComparison.Ordinal);
        Assert.Equal(GateStatus.Error, evaluation.Gate.Status);
    }

    [Fact]
    public void AFailedDiffIsAToolError()
    {
        using var root = new TempDirectory();
        root.Write("score.feature", Outline);

        var evaluation = GherkinGate.Evaluate(Plan(root.Path, "missing"), (_, _, _) => 0);

        Assert.Contains("git diff failed", evaluation.Error, StringComparison.Ordinal);
        Assert.Equal(GateStatus.Error, evaluation.Gate.Status);
    }

    [Fact]
    public void AFailingBaselineIsAToolError()
    {
        using var root = Bound(out _);

        var evaluation = GherkinGate.Evaluate(Plan(root.Path), (_, _, _) => 1);

        Assert.Equal("the acceptance project did not pass before mutation.", evaluation.Error);
        Assert.Equal(GateStatus.Error, evaluation.Gate.Status);
    }

    [Fact]
    public void AMissingDotnetOnTheBaselineIsAToolError()
    {
        using var root = Bound(out _);

        var evaluation = GherkinGate.Evaluate(Plan(root.Path), (_, _, _) => -1);

        Assert.Equal("dotnet executable was not found.", evaluation.Error);
        Assert.Equal(GateStatus.Error, evaluation.Gate.Status);
    }

    [Fact]
    public void AMissingDotnetDuringMutationIsAToolError()
    {
        using var root = Bound(out var feature);

        var evaluation = GherkinGate.Evaluate(
            Plan(root.Path),
            (_, _, scenario) => scenario is null ? 0 : -1
        );

        Assert.Equal("dotnet executable was not found.", evaluation.Error);
        Assert.Equal(GateStatus.Error, evaluation.Gate.Status);
        Assert.Contains("| 2     |", File.ReadAllText(feature), StringComparison.Ordinal);
    }

    [Fact]
    public void AScenarioWithoutExamplesPassesWithoutRunningTests()
    {
        using var root = new TempDirectory();
        root.Write(
            "plain.feature",
            """
            Feature: Score

              Scenario: Nothing to mutate
                Given a value of 2
            """
        );
        root.Write("steps/Steps.cs", """[Given("a value of {int}")] public class Steps { }""");
        var ran = false;

        var evaluation = GherkinGate.Evaluate(
            Plan(root.Path),
            (_, _, _) =>
            {
                ran = true;
                return 0;
            }
        );

        Assert.False(ran);
        Assert.Null(evaluation.Error);
        Assert.Equal(GateStatus.Pass, evaluation.Gate.Status);
    }

    private static TempDirectory Bound(out string feature)
    {
        var root = new TempDirectory();
        feature = Path.Combine(root.Path, "score.feature");
        root.Write("score.feature", Outline);
        root.Write(
            "steps/Steps.cs",
            """
            [Given("a value of {int}")]
            [Then("the value is positive")]
            public class Steps { }
            """
        );
        return root;
    }

    private const string Outline = """
        Feature: Score

          Scenario Outline: Positive stays positive
            Given a value of <value>
            Then the value is positive
            Examples:
              | value |
              | 2     |
        """;

    private static RunPlan Plan(string root, string? baseRef = null) =>
        new(
            ForgeVerbs.MutateGherkin,
            root,
            ["features"],
            baseRef,
            null,
            null,
            30,
            80,
            80,
            null,
            Path.Combine(root, "acceptance.csproj"),
            false,
            ["sonar"]
        );
}
