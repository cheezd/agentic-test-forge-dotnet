namespace AgenticTestForge.Reporting;

/// <summary>Advisory duplication finding. Either a pair or a Sonar issue key.</summary>
/// <param name="Advisory">Always true. DRY does not change the top-level status.</param>
/// <param name="SonarIssueKey">Sonar issue key, when the finding came from the scanner.</param>
/// <param name="Filepath">First file of a pair.</param>
/// <param name="QualifiedName">First member of a pair.</param>
/// <param name="DuplicateFilepath">Second file of a pair.</param>
/// <param name="DuplicateOf">Second member of a pair.</param>
internal sealed record DryFinding(
    bool Advisory,
    string? SonarIssueKey,
    string? Filepath,
    string? QualifiedName,
    string? DuplicateFilepath,
    string? DuplicateOf
);
