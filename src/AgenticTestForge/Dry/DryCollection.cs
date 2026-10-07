using AgenticTestForge.Reporting;

namespace AgenticTestForge.Dry;

/// <summary>What one source found. <see cref="Ran"/> is false when that source did not execute.</summary>
internal sealed record DryCollection(
    bool Ran,
    IReadOnlyList<DryFinding> Findings,
    DuplicationsCondition? Condition
)
{
    public static DryCollection NotRun { get; } = new(false, [], null);
}
