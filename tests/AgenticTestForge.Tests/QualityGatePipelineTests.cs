using AgenticTestForge.Cli;
using AgenticTestForge.Config;
using AgenticTestForge.Exit;
using AgenticTestForge.Reporting;

namespace AgenticTestForge.Tests;

[Trait("Category", "Unit")]
public class QualityGatePipelineTests
{
    [Fact]
    public void CheckKeepsGoingAfterAGateFailure()
    {
        var first = new ScriptedGate(
            "crap",
            new GateEvaluation(GateReport.Crap(30, GateStatus.Fail, []), null)
        );
        var second = new ScriptedGate(
            "mutation",
            new GateEvaluation(GateReport.Mutation(80, GateStatus.Fail, [], [], []), null)
        );

        var result = QualityGatePipeline.Run(Plan(ForgeVerbs.Check), [first, second]);

        Assert.Equal(ReportStatus.Fail, result.Status);
        Assert.Equal(ExitCodes.Fail, ExitCodes.For(result.Status));
        Assert.True(first.Ran);
        Assert.True(second.Ran);
        Assert.Equal(["crap", "mutation"], result.Report!.Gates.Select(static gate => gate.Name));
    }

    [Fact]
    public void AToolErrorStillRunsLaterGatesAndForcesExitTwo()
    {
        var first = new ScriptedGate(
            "crap",
            new GateEvaluation(
                GateReport.Crap(30, GateStatus.Error, []),
                "coverage could not be produced."
            )
        );
        var second = new ScriptedGate(
            "mutation",
            new GateEvaluation(GateReport.Mutation(80, GateStatus.Pass, [], [], []), null)
        );

        var result = QualityGatePipeline.Run(Plan(ForgeVerbs.Check), [first, second]);

        Assert.Equal(ReportStatus.Error, result.Status);
        Assert.Equal(ExitCodes.Error, ExitCodes.For(result.Status));
        Assert.True(second.Ran);
        Assert.Equal(["crap", "mutation"], result.Report!.Gates.Select(static gate => gate.Name));
        Assert.Contains("coverage could not be produced.", result.Errors);
    }

    [Fact]
    public void CrapVerbRunsOnlyTheCrapGate()
    {
        var crap = new ScriptedGate(
            "crap",
            new GateEvaluation(GateReport.Crap(30, GateStatus.Pass, []), null)
        );
        var mutation = new ScriptedGate(
            "mutation",
            new GateEvaluation(GateReport.Mutation(80, GateStatus.Fail, [], [], []), null)
        );

        var result = QualityGatePipeline.Run(Plan(ForgeVerbs.Crap), [crap, mutation]);

        Assert.Equal(ReportStatus.Pass, result.Status);
        Assert.True(crap.Ran);
        Assert.False(mutation.Ran);
    }

    private static RunPlan Plan(string verb) =>
        new(verb, "repo", ["src"], null, null, null, 30, 80, 80, "tests.csproj", null, true);

    private sealed class ScriptedGate(string verb, GateEvaluation evaluation) : IQualityGate
    {
        public bool Ran { get; private set; }

        public bool RunsFor(string candidate) => candidate is ForgeVerbs.Check || candidate == verb;

        public GateEvaluation Evaluate(RunPlan plan)
        {
            Ran = true;
            return evaluation;
        }
    }
}
