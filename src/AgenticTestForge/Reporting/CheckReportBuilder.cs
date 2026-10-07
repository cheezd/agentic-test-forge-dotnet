using AgenticTestForge.Cli;

namespace AgenticTestForge.Reporting;

internal static class CheckReportBuilder
{
    public static RunResult ForToolError(ParsedArgs parsed, string error) =>
        Finish(parsed.Verb ?? "forge", parsed.BaseRef, parsed.Paths, [], [error], toolError: true);

    public static RunResult Finish(
        string tool,
        string? baseRef,
        IReadOnlyList<string> paths,
        IReadOnlyList<IGateReport> gates,
        IReadOnlyList<string> errors,
        bool toolError
    )
    {
        var outcomes = gates
            .Select(static gate => new GateOutcome(gate.Name, gate.Status))
            .ToArray();
        var result = RunResult.From(outcomes, toolError, errors);
        var report = CheckReport.Create(tool, result.Status, baseRef, paths, gates, errors);
        return result with { Report = report };
    }
}
