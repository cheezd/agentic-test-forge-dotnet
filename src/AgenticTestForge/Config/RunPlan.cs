namespace AgenticTestForge.Config;

/// <summary>One verb after <c>forge.json</c> and the shared flags are combined.</summary>
/// <param name="Verb">Command that is running.</param>
/// <param name="RepoRoot">Directory that contains <c>forge.json</c>.</param>
/// <param name="Paths">Source roots. Command <c>--path</c> values replace the configured roots.</param>
/// <param name="BaseRef">Git ref for <c>--base</c>, when set.</param>
/// <param name="ScopedFiles">Changed files inside <paramref name="Paths"/> when <c>--base</c> is set; otherwise null.</param>
/// <param name="JsonPath">Report path for <c>--json</c>, when set.</param>
/// <param name="CrapThreshold">Ceiling for this run.</param>
/// <param name="MutationThreshold">Kill-rate floor for this run.</param>
/// <param name="GherkinThreshold">Gherkin floor for this run.</param>
/// <param name="TestProject">Absolute unit-test project path, when the verb resolved one.</param>
/// <param name="AcceptanceProject">Absolute acceptance project path, when one is configured and resolved.</param>
/// <param name="SkipGherkin">True when <c>acceptance_project</c> is omitted. Skipped Gherkin does not fail the run.</param>
internal sealed record RunPlan(
    string Verb,
    string RepoRoot,
    IReadOnlyList<string> Paths,
    string? BaseRef,
    IReadOnlyList<string>? ScopedFiles,
    string? JsonPath,
    double CrapThreshold,
    double MutationThreshold,
    double GherkinThreshold,
    string? TestProject,
    string? AcceptanceProject,
    bool SkipGherkin
);
