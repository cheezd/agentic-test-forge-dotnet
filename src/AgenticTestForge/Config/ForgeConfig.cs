namespace AgenticTestForge.Config;

/// <summary>Consumer settings from <c>forge.json</c> after defaults are applied.</summary>
/// <param name="Paths">Source roots. Empty when the file omits <c>paths</c>.</param>
/// <param name="TestProject">Unit-test project, relative to the repo root, or null when omitted.</param>
/// <param name="AcceptanceProject">Acceptance project, or null when Gherkin is not configured.</param>
/// <param name="CrapThreshold">CRAP ceiling. Default is 30.</param>
/// <param name="MutationThreshold">Mutation kill-rate floor. Default is 80.</param>
/// <param name="GherkinThreshold">Gherkin mutation floor. Default is 80.</param>
/// <param name="DrySources">Duplication source ids. Default is <c>sonar</c>.</param>
internal sealed record ForgeConfig(
    IReadOnlyList<string> Paths,
    string? TestProject,
    string? AcceptanceProject,
    double CrapThreshold,
    double MutationThreshold,
    double GherkinThreshold,
    IReadOnlyList<string> DrySources
)
{
    public const double DefaultCrapThreshold = 30;
    public const double DefaultMutationThreshold = 80;
    public const double DefaultGherkinThreshold = 80;

    public static IReadOnlyList<string> DefaultDrySources { get; } = ["sonar"];
}
