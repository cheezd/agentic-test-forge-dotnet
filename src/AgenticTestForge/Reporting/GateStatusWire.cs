namespace AgenticTestForge.Reporting;

internal static class GateStatusWire
{
    public static string ToWire(this GateStatus status) =>
        status switch
        {
            GateStatus.Pass => "pass",
            GateStatus.Fail => "fail",
            GateStatus.Advisory => "advisory",
            GateStatus.Skipped => "skipped",
            GateStatus.Error => "error",
            _ => throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Unknown gate status."
            ),
        };
}
