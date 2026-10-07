using AgenticTestForge.Reporting;

namespace AgenticTestForge.Crap;

internal sealed record CrapEvaluation(IGateReport Gate, string? Error)
{
    public static CrapEvaluation Ok(IGateReport gate) => new(gate, null);

    public static CrapEvaluation ToolError(double threshold, string error) =>
        new(GateReport.Crap(threshold, GateStatus.Error, []), error);
}
