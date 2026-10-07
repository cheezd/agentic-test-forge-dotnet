namespace AgenticTestForge.Config;

/// <summary>A resolved run, or the tool error that stops it.</summary>
/// <param name="Plan">Combined settings when resolution succeeded.</param>
/// <param name="Error">Tool error. The process exits 2.</param>
internal sealed record ResolveOutcome(RunPlan? Plan, string? Error)
{
    public static ResolveOutcome Ok(RunPlan plan) => new(plan, null);

    public static ResolveOutcome Failed(string error) => new(null, error);
}
