using AgenticTestForge.Exit;
using AgenticTestForge.Reporting;

namespace AgenticTestForge.Tests;

[Trait("Category", "Unit")]
public class RunResultTests
{
    [Fact]
    public void PassesWhenEveryHardGateThatRanPassed()
    {
        var result = RunResult.From([
            new GateOutcome("crap", GateStatus.Pass),
            new GateOutcome("mutation", GateStatus.Pass),
            new GateOutcome("dry", GateStatus.Advisory),
            new GateOutcome("gherkin", GateStatus.Skipped),
        ]);

        Assert.Equal(ReportStatus.Pass, result.Status);
        Assert.Equal(ExitCodes.Pass, ExitCodes.For(result.Status));
    }

    [Fact]
    public void PassesWhenNoHardGateRan()
    {
        var result = RunResult.From([]);

        Assert.Equal(ReportStatus.Pass, result.Status);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void FailsWhenAHardGateMissesItsCeilingOrFloor()
    {
        var result = RunResult.From([
            new GateOutcome("crap", GateStatus.Fail),
            new GateOutcome("dry", GateStatus.Advisory),
            new GateOutcome("gherkin", GateStatus.Skipped),
        ]);

        Assert.Equal(ReportStatus.Fail, result.Status);
        Assert.Equal(ExitCodes.Fail, ExitCodes.For(result.Status));
    }

    [Fact]
    public void ToolErrorOutranksAGateFailure()
    {
        var result = RunResult.From(
            [new GateOutcome("crap", GateStatus.Fail)],
            toolError: true,
            errors: ["coverage was not produced"]
        );

        Assert.Equal(ReportStatus.Error, result.Status);
        Assert.Equal(ExitCodes.Error, ExitCodes.For(result.Status));
        Assert.Equal("coverage was not produced", Assert.Single(result.Errors));
    }

    [Fact]
    public void GateErrorOutranksAGateFailure()
    {
        var result = RunResult.From([
            new GateOutcome("crap", GateStatus.Fail),
            new GateOutcome("mutation", GateStatus.Error),
        ]);

        Assert.Equal(ReportStatus.Error, result.Status);
        Assert.Equal(ExitCodes.Error, ExitCodes.For(result.Status));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AdvisoryAndSkippedLeaveAPassingRunAtZero(bool includePass)
    {
        var gates = new List<GateOutcome>
        {
            new("dry", GateStatus.Advisory),
            new("gherkin", GateStatus.Skipped),
        };
        if (includePass)
        {
            gates.Add(new GateOutcome("crap", GateStatus.Pass));
        }

        var result = RunResult.From(gates);

        Assert.Equal(ReportStatus.Pass, result.Status);
        Assert.Equal(0, ExitCodes.For(result.Status));
    }
}
