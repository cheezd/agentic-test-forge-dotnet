using System.Text.Json;
using AgenticTestForge.Cli;
using AgenticTestForge.Exit;
using AgenticTestForge.Reporting;

namespace AgenticTestForge.Tests;

[Trait("Category", "Unit")]
public class CheckReportTests
{
    [Fact]
    public void CheckEnvelopeKeepsThePythonKeysAndAddsVersionAndScope()
    {
        var report = CheckReport.Create(
            "check",
            ReportStatus.Pass,
            baseRef: null,
            paths: ["src/App"],
            gates:
            [
                GateReport.Crap(30, GateStatus.Pass, []),
                GateReport.Mutation(80, GateStatus.Pass, [], [], []),
                GateReport.Gherkin(80, GateStatus.Skipped),
                GateReport.Dry(GateStatus.Skipped, []),
            ],
            errors: []
        );

        using var document = JsonDocument.Parse(report.ToJson());
        var root = document.RootElement;
        Assert.Equal(
            [
                "tool",
                "version",
                "status",
                "summary",
                "scope",
                "gates_run",
                "gate_policies",
                "errors",
                "reports",
            ],
            root.EnumerateObject().Select(static property => property.Name).ToArray()
        );
        Assert.Equal("check", root.GetProperty("tool").GetString());
        Assert.Equal(ToolVersion.Current, root.GetProperty("version").GetString());
        Assert.Equal("pass", root.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("scope").GetProperty("base").ValueKind);
        Assert.Equal("src/App", root.GetProperty("scope").GetProperty("paths")[0].GetString());
        Assert.Equal(["crap", "mutation", "gherkin", "dry"], Names(root.GetProperty("gates_run")));
        Assert.Equal("blocking", root.GetProperty("gate_policies").GetProperty("crap").GetString());
        Assert.Equal("advisory", root.GetProperty("gate_policies").GetProperty("dry").GetString());
        Assert.Equal(
            "skipped",
            root.GetProperty("reports").GetProperty("gherkin").GetProperty("status").GetString()
        );
        Assert.Equal(
            80,
            root.GetProperty("reports").GetProperty("gherkin").GetProperty("threshold").GetDouble()
        );
        Assert.Equal(
            "skipped",
            root.GetProperty("reports").GetProperty("dry").GetProperty("status").GetString()
        );
        Assert.True(
            root.GetProperty("reports").GetProperty("dry").GetProperty("advisory").GetBoolean()
        );
    }

    [Fact]
    public void SkippedAndAdvisoryDoNotFailTheReport()
    {
        var result = CheckReportBuilder.Finish(
            "check",
            baseRef: "main",
            paths: ["src/App"],
            gates:
            [
                GateReport.Crap(30, GateStatus.Pass, []),
                GateReport.Gherkin(80, GateStatus.Skipped),
                GateReport.Dry(
                    GateStatus.Advisory,
                    [new DryFinding(true, "csharpsquid:S4144", null, null, null, null)]
                ),
            ],
            errors: [],
            toolError: false
        );

        Assert.Equal(ReportStatus.Pass, result.Status);
        Assert.Equal(ExitCodes.Pass, ExitCodes.For(result.Status));
        using var document = JsonDocument.Parse(result.Report!.ToJson());
        var dry = document
            .RootElement.GetProperty("reports")
            .GetProperty("dry")
            .GetProperty("findings")[0];
        Assert.True(dry.GetProperty("advisory").GetBoolean());
        Assert.Equal("csharpsquid:S4144", dry.GetProperty("sonar_issue_key").GetString());
        Assert.Equal("pass", document.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public void CrapFindingKeepsNullCoverageWhenTheMemberIsNotApplicable()
    {
        var finding = new CrapFinding(
            "App.Widget.Run",
            "src/App/Widget.cs",
            8,
            null,
            null,
            ThresholdRules.CrapIsAboveCeiling(null, 30)
        );
        var report = CheckReport.Create(
            "crap",
            ReportStatus.Fail,
            "main",
            ["src/App"],
            [GateReport.Crap(30, GateStatus.Fail, [finding])],
            []
        );

        using var document = JsonDocument.Parse(report.ToJson());
        var member = document
            .RootElement.GetProperty("reports")
            .GetProperty("crap")
            .GetProperty("findings")[0];
        Assert.Equal("App.Widget.Run", member.GetProperty("qualified_name").GetString());
        Assert.Equal("src/App/Widget.cs", member.GetProperty("filepath").GetString());
        Assert.Equal(8, member.GetProperty("complexity").GetDouble());
        Assert.Equal(JsonValueKind.Null, member.GetProperty("coverage").ValueKind);
        Assert.Equal(JsonValueKind.Null, member.GetProperty("crap_score").ValueKind);
        Assert.True(member.GetProperty("above_threshold").GetBoolean());
        Assert.Equal("fail", document.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public void MutationReportListsSurvivorsAndInconclusiveMutants()
    {
        var finding = new MutationFinding(
            "src/App/Widget.cs",
            50,
            1,
            2,
            ThresholdRules.MutationIsUnderFloor(50, 80)
        );
        var survivor = new MutantRef("src/App/Widget.cs", "App.Widget.Run", "statement");
        var inconclusive = new MutantRef("src/App/Widget.cs", "App.Widget.Stop", "equality");
        var result = CheckReportBuilder.Finish(
            "mutate",
            "main",
            ["src/App"],
            [GateReport.Mutation(80, GateStatus.Fail, [finding], [survivor], [inconclusive])],
            [],
            toolError: false
        );

        Assert.Equal(ReportStatus.Fail, result.Status);
        Assert.Equal(1, ExitCodes.For(result.Status));
        using var document = JsonDocument.Parse(result.Report!.ToJson());
        var mutation = document.RootElement.GetProperty("reports").GetProperty("mutation");
        var listed = mutation.GetProperty("findings")[0];
        Assert.Equal(50, listed.GetProperty("score").GetDouble());
        Assert.Equal(1, listed.GetProperty("killed").GetInt32());
        Assert.Equal(2, listed.GetProperty("total").GetInt32());
        Assert.True(listed.GetProperty("above_threshold").GetBoolean());
        Assert.Equal(
            "statement",
            mutation.GetProperty("survivors")[0].GetProperty("mutator").GetString()
        );
        Assert.Equal(
            "App.Widget.Stop",
            mutation.GetProperty("inconclusive")[0].GetProperty("qualified_name").GetString()
        );
        Assert.Equal("fail", document.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public void AScoreOnTheCeilingOrFloorIsNotABreach()
    {
        Assert.False(ThresholdRules.CrapIsAboveCeiling(30, 30));
        Assert.True(ThresholdRules.CrapIsAboveCeiling(30.1, 30));
        Assert.False(ThresholdRules.MutationIsUnderFloor(80, 80));
        Assert.True(ThresholdRules.MutationIsUnderFloor(79.9, 80));
    }

    [Fact]
    public void ToolErrorOutranksAFailingGateInTheDocument()
    {
        var result = CheckReportBuilder.Finish(
            "check",
            null,
            ["src/App"],
            [GateReport.Crap(30, GateStatus.Fail, [])],
            ["coverage was not produced"],
            toolError: true
        );

        Assert.Equal(ReportStatus.Error, result.Status);
        Assert.Equal(2, ExitCodes.For(result.Status));
        using var document = JsonDocument.Parse(result.Report!.ToJson());
        Assert.Equal("error", document.RootElement.GetProperty("status").GetString());
        Assert.Equal(
            "coverage was not produced",
            document.RootElement.GetProperty("errors")[0].GetString()
        );
    }

    [Fact]
    public void WrittenReportStatusAgreesWithTheProcessExitCode()
    {
        using var root = new TempDirectory();
        var reportPath = Path.Combine(root.Path, "report.json");
        var result = CheckReportBuilder.Finish(
            "crap",
            "main",
            ["src/App"],
            [
                GateReport.Crap(
                    30,
                    GateStatus.Fail,
                    [new CrapFinding("App.Widget.Run", "src/App/Widget.cs", 8, 0.1, 40, true)]
                ),
            ],
            [],
            toolError: false
        );
        var stdout = new StringWriter();

        var code = ForgeApplication.Run(
            ["crap", "--json", reportPath],
            stdout,
            new StringWriter(),
            new FakeGateRunner(result)
        );

        Assert.Equal(ExitCodes.Fail, code);
        Assert.Contains("crap: fail", stdout.ToString(), StringComparison.Ordinal);
        using var document = JsonDocument.Parse(File.ReadAllText(reportPath));
        Assert.Equal("fail", document.RootElement.GetProperty("status").GetString());
    }

    private static string[] Names(JsonElement array) =>
        array.EnumerateArray().Select(static item => item.GetString()!).ToArray();
}
