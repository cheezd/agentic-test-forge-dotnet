using System.Text.Json.Nodes;

namespace AgenticTestForge.Reporting;

internal sealed record MutationGateReport(
    GateStatus Status,
    double? Threshold,
    IReadOnlyList<MutationFinding> Findings,
    IReadOnlyList<MutantRef> Survivors,
    IReadOnlyList<MutantRef> Inconclusive
) : IGateReport
{
    public string Name => "mutation";

    public string Policy => "blocking";

    public bool Advisory => false;

    public JsonObject ToJson()
    {
        var node = ReportJson.Open(Status, Threshold, Advisory);
        node["findings"] = ReportJson.MutationFindings(Findings);
        node["survivors"] = ReportJson.Mutants(Survivors);
        node["inconclusive"] = ReportJson.Mutants(Inconclusive);
        return node;
    }
}
