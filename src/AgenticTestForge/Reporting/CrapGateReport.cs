using System.Text.Json.Nodes;

namespace AgenticTestForge.Reporting;

internal sealed record CrapGateReport(
    GateStatus Status,
    double? Threshold,
    IReadOnlyList<CrapFinding> Findings
) : IGateReport
{
    public string Name => "crap";

    public string Policy => "blocking";

    public bool Advisory => false;

    public JsonObject ToJson()
    {
        var node = ReportJson.Open(Status, Threshold, Advisory);
        node["findings"] = ReportJson.CrapFindings(Findings);
        return node;
    }
}
