using System.Text.Json;
using AgenticTestForge.Cli;
using AgenticTestForge.Config;
using AgenticTestForge.Dry;
using AgenticTestForge.Execution;
using AgenticTestForge.Exit;
using AgenticTestForge.Reporting;

namespace AgenticTestForge.Tests;

[Trait("Category", "Unit")]
public class DrySourceTests
{
    [Fact]
    public void AMissingScannerSkipsTheGate()
    {
        var evaluation = DryGate.Evaluate(
            Plan(),
            [new SonarDrySource(_ => false, Settings, (_, _) => null)]
        );

        Assert.Null(evaluation.Error);
        Assert.Equal(GateStatus.Skipped, evaluation.Report.Status);
        Assert.Empty(Assert.IsType<DryGateReport>(evaluation.Report).Findings);
    }

    [Fact]
    public void SonarFindingsStayAdvisoryAndKeepTheIssueKey()
    {
        var source = new SonarDrySource(_ => true, Settings, (_, _) => SamplePayload());
        var jaccard = new FixedSource(
            new DryCollection(
                true,
                [
                    new DryFinding(
                        true,
                        null,
                        "src/A.cs",
                        "App.A.Run",
                        "src/B.cs",
                        "App.B.Run",
                        "jaccard"
                    ),
                ],
                null
            )
        );

        var evaluation = DryGate.Evaluate(Plan(), [source, jaccard]);
        var result = CheckReportBuilder.Finish(
            "check",
            null,
            ["src"],
            [GateReport.Crap(30, GateStatus.Pass, []), evaluation.Report],
            [],
            false
        );

        Assert.Null(evaluation.Error);
        Assert.Equal(ReportStatus.Pass, result.Status);
        Assert.Equal(ExitCodes.Pass, ExitCodes.For(result.Status));
        using var document = JsonDocument.Parse(result.Report!.ToJson());
        var dry = document.RootElement.GetProperty("reports").GetProperty("dry");
        Assert.Equal("advisory", dry.GetProperty("status").GetString());
        Assert.Equal(
            "duplicated_lines_density",
            dry.GetProperty("duplications_condition").GetProperty("metric").GetString()
        );
        Assert.Equal(
            "1.5",
            dry.GetProperty("duplications_condition").GetProperty("actual").GetString()
        );
        var findings = dry.GetProperty("findings");
        Assert.Equal(2, findings.GetArrayLength());
        Assert.Equal("AY123", findings[0].GetProperty("sonar_issue_key").GetString());
        Assert.Equal("src/Foo.cs", findings[0].GetProperty("filepath").GetString());
        Assert.Equal("sonar", findings[0].GetProperty("source").GetString());
        Assert.Equal("jaccard", findings[1].GetProperty("source").GetString());
        Assert.Equal("App.B.Run", findings[1].GetProperty("duplicate_of").GetString());
    }

    [Fact]
    public void JaccardAloneSkipsUntilItIsRegistered()
    {
        var evaluation = DryGate.Evaluate(Plan() with { DrySources = ["jaccard"] });

        Assert.Null(evaluation.Error);
        Assert.Equal(GateStatus.Skipped, evaluation.Report.Status);
    }

    [Fact]
    public void UnreadableSonarOutputDoesNotRun()
    {
        var collection = SonarReport.Read(new SonarPayload("not-json", "{}"));

        Assert.False(collection.Ran);
    }

    [Fact]
    public void AnUnregisteredSourceDoesNotRun()
    {
        Assert.Empty(DrySourceCatalog.Select(["jaccard"]));
        Assert.Equal(
            ["sonar"],
            DrySourceCatalog.Select(["Sonar", "jaccard"]).Select(static source => source.Id)
        );
    }

    [Fact]
    public void TheScannerProbeAcceptsTheFirstExecutableThatStarts()
    {
        var calls = new List<string>();
        var available = SonarProcess.IsAvailable(
            "repo",
            (file, _, _) =>
            {
                calls.Add(file);
                return file == "sonar-scanner.bat"
                    ? new ProcessOutput(0, "scanner", "")
                    : new ProcessOutput(-1, "", "missing");
            }
        );

        Assert.True(available);
        Assert.Equal(["sonar-scanner", "sonar-scanner.exe", "sonar-scanner.bat"], calls);
    }

    [Fact]
    public void CurlReadsTheQualityGateAndTheIssueList()
    {
        var settings = new SonarSettings("https://sonar.example", "token", "app");
        var urls = new List<string>();
        var payload = SonarProcess.Fetch(
            settings,
            "repo",
            (_, _, arguments) =>
            {
                urls.Add(arguments[^1]);
                return new ProcessOutput(0, "{}", "");
            }
        );

        Assert.NotNull(payload);
        Assert.Contains(
            "https://sonar.example/api/qualitygates/project_status?projectKey=app",
            urls
        );
        Assert.Contains(
            "https://sonar.example/api/issues/search?componentKeys=app&ps=500&statuses=OPEN,CONFIRMED",
            urls
        );
    }

    [Fact]
    public void AFailedFetchDoesNotRun()
    {
        var source = new SonarDrySource(_ => true, Settings, (_, _) => null);

        var collection = source.Collect(Plan());

        Assert.False(collection.Ran);
    }

    private static SonarSettings? Settings() => new("https://sonar.example", "token", "app");

    private static SonarPayload SamplePayload() =>
        new(
            """
            {
              "projectStatus": {
                "conditions": [
                  { "metricKey": "coverage", "status": "OK", "actualValue": "80" },
                  { "metricKey": "new_duplicated_lines_density", "status": "OK", "actualValue": "4" },
                  { "metricKey": "duplicated_lines_density", "status": "ERROR", "actualValue": "1.5" }
                ]
              }
            }
            """,
            """
            {
              "issues": [
                {
                  "key": "AY123",
                  "rule": "csharpsquid:S4144",
                  "component": "app:src/Foo.cs",
                  "message": "Update this method so that its implementation is not identical."
                },
                { "key": "AY999", "rule": "csharpsquid:S100", "component": "app:src/Bar.cs", "message": "Rename this method." }
              ]
            }
            """
        );

    private static RunPlan Plan() =>
        new(
            ForgeVerbs.Dry,
            "repo",
            ["src"],
            null,
            null,
            null,
            30,
            80,
            80,
            null,
            null,
            true,
            ["sonar", "jaccard"]
        );

    private sealed class FixedSource(DryCollection collection) : IDrySource
    {
        public string Id => "jaccard";

        public DryCollection Collect(RunPlan plan) => collection;
    }
}
