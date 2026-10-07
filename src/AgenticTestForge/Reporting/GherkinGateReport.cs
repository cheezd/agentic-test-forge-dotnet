using System.Text.Json.Nodes;

namespace AgenticTestForge.Reporting;

internal sealed record GherkinGateReport(GateStatus Status, double? Threshold) : IGateReport
{
    public string Name => "gherkin";

    public string Policy => "blocking";

    public bool Advisory => false;

    public JsonObject ToJson()
    {
        var node = ReportJson.Open(Status, Threshold, Advisory);
        node["findings"] = new JsonArray();
        return node;
    }
}
