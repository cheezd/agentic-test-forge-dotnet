using System.Text.Json;
using AgenticTestForge.Cli;
using AgenticTestForge.Config;
using AgenticTestForge.Dry;
using AgenticTestForge.Exit;
using AgenticTestForge.Reporting;

namespace AgenticTestForge.Tests;

[Trait("Category", "Unit")]
public class JaccardSimilarityTests
{
    [Fact]
    public void RenamedClonesReportAtOrAboveTheFloorAndStayAdvisory()
    {
        using var root = new TempDirectory();
        root.Write("src/Left.cs", Clone("Left", "Total", "left", "right", "total"));
        root.Write("src/Right.cs", Clone("Right", "Sum", "first", "second", "sum"));
        root.Write("src/Short.cs", "class Short { public static int One(int value) => value; }");
        var plan = new RunPlan(
            ForgeVerbs.Dry,
            root.Path,
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
            ["jaccard"]
        );

        var evaluation = DryGate.Evaluate(plan);
        var result = CheckReportBuilder.Finish(
            "check",
            null,
            ["src"],
            [evaluation.Report],
            [],
            false
        );

        Assert.Null(evaluation.Error);
        Assert.Equal(GateStatus.Advisory, evaluation.Report.Status);
        Assert.Equal(ReportStatus.Pass, result.Status);
        Assert.Equal(ExitCodes.Pass, ExitCodes.For(result.Status));
        var finding = Assert.Single(Assert.IsType<DryGateReport>(evaluation.Report).Findings);
        Assert.Equal("jaccard", finding.Source);
        Assert.True(finding.Similarity >= JaccardSimilarity.Threshold);
        Assert.Equal("src/Left.cs", finding.Filepath);
        Assert.Equal("Left.Total", finding.QualifiedName);
        Assert.Equal("src/Right.cs", finding.DuplicateFilepath);
        Assert.Equal("Right.Sum", finding.DuplicateOf);
        using var document = JsonDocument.Parse(result.Report!.ToJson());
        var json = document
            .RootElement.GetProperty("reports")
            .GetProperty("dry")
            .GetProperty("findings")[0];
        Assert.Equal(finding.Similarity, json.GetProperty("similarity_score").GetDouble());
    }

    [Fact]
    public void AScoreUnderTheFloorIsNotReported()
    {
        var left = Unit("src/A.cs", "A.Run", Shared(81).Concat(Only("l", 9)));
        var right = Unit("src/B.cs", "B.Run", Shared(81).Concat(Only("r", 9)));

        var findings = JaccardSimilarity.Score([left, right]);

        Assert.Empty(findings);
    }

    [Fact]
    public void AScoreEqualToTheFloorIsReported()
    {
        var left = Unit("src/A.cs", "A.Run", Shared(82).Concat(Only("l", 9)));
        var right = Unit("src/B.cs", "B.Run", Shared(82).Concat(Only("r", 9)));

        var finding = Assert.Single(JaccardSimilarity.Score([left, right]));

        Assert.Equal(0.82, finding.Similarity ?? 0, precision: 2);
    }

    [Fact]
    public void MethodsUnderTheSizeFilterAreSkipped()
    {
        var left = Unit("src/A.cs", "A.Run", Shared(10), lines: 2, nodes: 5);
        var right = Unit("src/B.cs", "B.Run", Shared(10), lines: 2, nodes: 5);

        Assert.Empty(JaccardSimilarity.Score([left, right]));
    }

    private static string Clone(
        string type,
        string method,
        string left,
        string right,
        string total
    ) =>
        $$"""
            public static class {{type}}
            {
                public static int {{method}}(int {{left}}, int {{right}})
                {
                    var {{total}} = {{left}} + {{right}};
                    if ({{total}} > {{left}})
                    {
                        {{total}} = {{total}} - {{right}};
                        {{total}} = {{total}} + 1;
                        return {{total}};
                    }

                    if ({{right}} > 0)
                    {
                        {{total}} = {{left}} * {{right}};
                        {{total}} = {{total}} / 2;
                        return {{total}};
                    }

                    {{total}} = {{left}} - {{right}};
                    return {{total}} + 0;
                }
            }
            """;

    private static MethodUnit Unit(
        string filepath,
        string name,
        IEnumerable<string> fingerprints,
        int lines = 4,
        int nodes = 20
    ) => new(name, filepath, lines, nodes, fingerprints.ToHashSet(StringComparer.Ordinal));

    private static IEnumerable<string> Shared(int count) =>
        Enumerable.Range(0, count).Select(static index => "s" + index);

    private static IEnumerable<string> Only(string prefix, int count) =>
        Enumerable.Range(0, count).Select(index => prefix + index);
}
