using AgenticTestForge.Reporting;

namespace AgenticTestForge.Mutation;

internal sealed record MutationEvaluation(IGateReport Gate, string? Error)
{
    public static MutationEvaluation Ok(IGateReport gate) => new(gate, null);

    public static MutationEvaluation ToolError(double threshold, string error) =>
        new(GateReport.Mutation(threshold, GateStatus.Error, [], [], []), error);
}
