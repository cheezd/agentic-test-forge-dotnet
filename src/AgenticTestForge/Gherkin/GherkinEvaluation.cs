using AgenticTestForge.Reporting;

namespace AgenticTestForge.Gherkin;

internal sealed record GherkinEvaluation(IGateReport Gate, string? Error)
{
    public static GherkinEvaluation Ok(IGateReport gate) => new(gate, null);

    public static GherkinEvaluation ToolError(double threshold, string error) =>
        new(GateReport.Gherkin(threshold, GateStatus.Error, [], [], []), error);
}
