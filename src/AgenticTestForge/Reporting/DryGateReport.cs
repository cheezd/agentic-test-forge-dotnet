using System.Text.Json.Nodes;

namespace AgenticTestForge.Reporting;

internal sealed record DryGateReport(GateStatus Status, IReadOnlyList<DryFinding> Findings)
    : IGateReport
{
    public string Name => "dry";

    public string Policy => "advisory";

    public double? Threshold => null;

    public bool Advisory => true;

    public JsonObject ToJson()
    {
        var node = ReportJson.Open(Status, Threshold, Advisory);
        node["findings"] = ReportJson.DryFindings(Findings);
        return node;
    }
}
