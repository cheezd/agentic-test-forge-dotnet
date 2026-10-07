namespace AgenticTestForge.Reporting;

/// <summary>Aggregate mutation result for one file.</summary>
/// <param name="Filepath">Repo-relative path.</param>
/// <param name="Score">Kill rate from 0 to 100.</param>
/// <param name="Killed">Killed mutants. A mutant that does not compile counts as killed.</param>
/// <param name="Total">Killed plus survived plus inconclusive.</param>
/// <param name="AboveThreshold">True when <paramref name="Score"/> is under the floor.</param>
internal sealed record MutationFinding(
    string Filepath,
    double Score,
    int Killed,
    int Total,
    bool AboveThreshold
);
