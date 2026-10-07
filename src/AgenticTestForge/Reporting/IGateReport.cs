using System.Text.Json.Nodes;

namespace AgenticTestForge.Reporting;

internal interface IGateReport
{
    string Name { get; }

    GateStatus Status { get; }

    string Policy { get; }

    double? Threshold { get; }

    bool Advisory { get; }

    JsonObject ToJson();
}
