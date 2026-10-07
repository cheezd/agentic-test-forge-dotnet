using AgenticTestForge.Reporting;

namespace AgenticTestForge.Exit;

/// <summary>
/// Process exit codes. Pass is 0, fail is 1, and error is 2.
/// A tool error outranks a gate failure.
/// </summary>
internal static class ExitCodes
{
    public const int Pass = 0;
    public const int Fail = 1;
    public const int Error = 2;

    public static int For(ReportStatus status) =>
        status switch
        {
            ReportStatus.Pass => Pass,
            ReportStatus.Fail => Fail,
            ReportStatus.Error => Error,
            _ => throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Unknown report status."
            ),
        };
}
