namespace AgenticTestForge.Reporting;

internal static class ReportStatusWire
{
    public static string ToWire(this ReportStatus status) =>
        status switch
        {
            ReportStatus.Pass => "pass",
            ReportStatus.Fail => "fail",
            ReportStatus.Error => "error",
            _ => throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Unknown report status."
            ),
        };
}
