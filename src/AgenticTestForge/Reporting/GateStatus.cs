namespace AgenticTestForge.Reporting;

/// <summary>Per-gate status. Advisory and skipped leave the top-level status unchanged.</summary>
internal enum GateStatus
{
    Pass,
    Fail,
    Advisory,
    Skipped,
    Error,
}
