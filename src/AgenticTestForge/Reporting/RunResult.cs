namespace AgenticTestForge.Reporting;

/// <summary>Combined status for one verb. Built so a tool error outranks a gate failure.</summary>
/// <param name="Status">Top-level status.</param>
/// <param name="Errors">Tool errors. A non-empty list forces <see cref="ReportStatus.Error"/>.</param>
/// <param name="Report">JSON document for <c>--json</c>, when the verb produced one.</param>
internal sealed record RunResult(
    ReportStatus Status,
    IReadOnlyList<string> Errors,
    CheckReport? Report = null
)
{
    public static RunResult From(
        IReadOnlyList<GateOutcome> gates,
        bool toolError = false,
        IReadOnlyList<string>? errors = null,
        CheckReport? report = null
    )
    {
        var errorList = errors ?? [];
        var status = Resolve(gates, toolError, errorList);
        return new RunResult(status, errorList, report);
    }

    private static ReportStatus Resolve(
        IReadOnlyList<GateOutcome> gates,
        bool toolError,
        IReadOnlyList<string> errors
    )
    {
        if (
            toolError
            || errors.Count > 0
            || gates.Any(static gate => gate.Status == GateStatus.Error)
        )
        {
            return ReportStatus.Error;
        }

        if (gates.Any(static gate => gate.Status == GateStatus.Fail))
        {
            return ReportStatus.Fail;
        }

        return ReportStatus.Pass;
    }
}
