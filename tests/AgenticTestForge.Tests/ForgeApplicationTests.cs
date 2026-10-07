using AgenticTestForge.Cli;
using AgenticTestForge.Exit;
using AgenticTestForge.Reporting;

namespace AgenticTestForge.Tests;

[Trait("Category", "Unit")]
public class ForgeApplicationTests
{
    [Fact]
    public void NoArgsPrintsHelpAndExitsZero()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var runner = new FakeGateRunner(RunResult.From([]));

        var code = ForgeApplication.Run([], stdout, stderr, runner);

        Assert.Equal(ExitCodes.Pass, code);
        Assert.False(runner.Called);
        Assert.Contains("forge", stdout.ToString(), StringComparison.Ordinal);
        Assert.Contains(ForgeVerbs.Check, stdout.ToString(), StringComparison.Ordinal);
        Assert.Contains(ForgeVerbs.Crap, stdout.ToString(), StringComparison.Ordinal);
        Assert.Contains(ForgeVerbs.Mutate, stdout.ToString(), StringComparison.Ordinal);
        Assert.Contains(ForgeVerbs.MutateGherkin, stdout.ToString(), StringComparison.Ordinal);
        Assert.Contains(ForgeVerbs.Dry, stdout.ToString(), StringComparison.Ordinal);
        Assert.Contains("--base", stdout.ToString(), StringComparison.Ordinal);
        Assert.Contains("--json", stdout.ToString(), StringComparison.Ordinal);
        Assert.Contains("--threshold", stdout.ToString(), StringComparison.Ordinal);
        Assert.Equal("", stderr.ToString());
    }

    [Theory]
    [InlineData("check")]
    [InlineData("crap")]
    [InlineData("mutate")]
    [InlineData("mutate-gherkin")]
    [InlineData("dry")]
    public void VerbExitsZeroWhenHardGatesPass(string verb)
    {
        var stdout = new StringWriter();
        var code = ForgeApplication.Run(
            [verb],
            stdout,
            new StringWriter(),
            new FakeGateRunner(
                RunResult.From([
                    new GateOutcome("crap", GateStatus.Pass),
                    new GateOutcome("dry", GateStatus.Advisory),
                    new GateOutcome("gherkin", GateStatus.Skipped),
                ])
            )
        );

        Assert.Equal(0, code);
        Assert.Contains($"{verb}: pass", stdout.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("check")]
    [InlineData("crap")]
    [InlineData("mutate")]
    [InlineData("mutate-gherkin")]
    [InlineData("dry")]
    public void VerbExitsOneWhenAHardGateFails(string verb)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var code = ForgeApplication.Run(
            [verb],
            stdout,
            stderr,
            new FakeGateRunner(RunResult.From([new GateOutcome("crap", GateStatus.Fail)]))
        );

        Assert.Equal(1, code);
        Assert.Contains($"{verb}: fail", stdout.ToString(), StringComparison.Ordinal);
        Assert.Equal("", stderr.ToString());
    }

    [Theory]
    [InlineData("check")]
    [InlineData("crap")]
    [InlineData("mutate")]
    [InlineData("mutate-gherkin")]
    [InlineData("dry")]
    public void VerbExitsTwoWhenTheRunnerReportsAToolError(string verb)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var code = ForgeApplication.Run(
            [verb],
            stdout,
            stderr,
            new FakeGateRunner(
                RunResult.From(
                    [new GateOutcome("crap", GateStatus.Fail)],
                    toolError: true,
                    errors: ["the test project cannot be resolved"]
                )
            )
        );

        Assert.Equal(2, code);
        Assert.Contains($"{verb}: error", stdout.ToString(), StringComparison.Ordinal);
        Assert.Contains(
            "the test project cannot be resolved",
            stderr.ToString(),
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void UnknownCommandExitsTwoAndPrintsHelp()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var runner = new FakeGateRunner(RunResult.From([]));

        var code = ForgeApplication.Run(["nope"], stdout, stderr, runner);

        Assert.Equal(ExitCodes.Error, code);
        Assert.False(runner.Called);
        Assert.Contains("Unknown command", stderr.ToString(), StringComparison.Ordinal);
        Assert.Contains(ForgeVerbs.Check, stdout.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void InvalidOptionExitsTwo()
    {
        var stderr = new StringWriter();
        var runner = new FakeGateRunner(RunResult.From([]));

        var code = ForgeApplication.Run(
            ["check", "--threshold", "30"],
            new StringWriter(),
            stderr,
            runner
        );

        Assert.Equal(ExitCodes.Error, code);
        Assert.False(runner.Called);
        Assert.Contains("--threshold", stderr.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void PassesParsedFlagsToTheRunner()
    {
        var runner = new FakeGateRunner(RunResult.From([]));

        var code = ForgeApplication.Run(
            [
                "check",
                "--base",
                "main",
                "--path",
                "src/App",
                "--path",
                "src/Other",
                "--json",
                "report.json",
            ],
            new StringWriter(),
            new StringWriter(),
            runner
        );

        Assert.Equal(ExitCodes.Pass, code);
        Assert.NotNull(runner.Invocation);
        Assert.Equal("main", runner.Invocation.BaseRef);
        Assert.Equal(["src/App", "src/Other"], runner.Invocation.Paths);
        Assert.Equal("report.json", runner.Invocation.JsonPath);
    }
}
