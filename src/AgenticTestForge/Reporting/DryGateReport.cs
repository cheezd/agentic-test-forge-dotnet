using System.Text.Json.Nodes;

namespace AgenticTestForge.Reporting;

internal sealed record DryGateReport(
    GateStatus Status,
    IReadOnlyList<DryFinding> Findings,
    DuplicationsCondition? Condition = null
) : IGateReport
{
    public string Name => "dry";

    public string Policy => "advisory";

    public double? Threshold => null;

    public bool Advisory => true;

    public JsonObject ToJson()
    {
        var node = ReportJson.Open(Status, Threshold, Advisory);
        if (Condition is not null)
        {
            node["duplications_condition"] = new JsonObject
            {
                ["metric"] = Condition.Metric,
                ["status"] = Condition.Status,
                ["actual"] = ReportJson.NullOrString(Condition.Actual),
            };
        }

        node["findings"] = ReportJson.DryFindings(Findings);
        return node;
    }
}
