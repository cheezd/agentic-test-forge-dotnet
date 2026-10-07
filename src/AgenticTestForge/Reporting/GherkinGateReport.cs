using System.Text.Json.Nodes;

namespace AgenticTestForge.Reporting;

internal sealed record GherkinGateReport(
    GateStatus Status,
    double? Threshold,
    IReadOnlyList<GherkinFinding> Findings,
    IReadOnlyList<MutantRef> Survivors,
    IReadOnlyList<MutantRef> Inconclusive,
    bool IncludeOutcomes
) : IGateReport
{
    public string Name => "gherkin";

    public string Policy => "blocking";

    public bool Advisory => false;

    public JsonObject ToJson()
    {
        var node = ReportJson.Open(Status, Threshold, Advisory);
        node["findings"] = ReportJson.GherkinFindings(Findings);
        if (IncludeOutcomes)
        {
            node["survivors"] = ReportJson.Mutants(Survivors);
            node["inconclusive"] = ReportJson.Mutants(Inconclusive);
        }

        return node;
    }
}
