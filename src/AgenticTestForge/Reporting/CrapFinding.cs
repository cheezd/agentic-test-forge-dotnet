namespace AgenticTestForge.Reporting;

/// <summary>CRAP result for one member. <see cref="Coverage"/> is null when the member is N/A.</summary>
/// <param name="QualifiedName">Source member the score belongs to.</param>
/// <param name="Filepath">Repo-relative path.</param>
/// <param name="Complexity">Cyclomatic complexity.</param>
/// <param name="Coverage">Fraction from 0 to 1, or null when coverage data is missing.</param>
/// <param name="CrapScore">Score, or null when coverage is N/A.</param>
/// <param name="AboveThreshold">True when the score is over the ceiling, including N/A.</param>
internal sealed record CrapFinding(
    string QualifiedName,
    string Filepath,
    double Complexity,
    double? Coverage,
    double? CrapScore,
    bool AboveThreshold
);
