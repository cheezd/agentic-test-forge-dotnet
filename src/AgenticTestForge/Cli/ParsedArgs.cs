namespace AgenticTestForge.Cli;

/// <summary>Parsed forge invocation.</summary>
/// <param name="Help">True when the process should print help and exit 0.</param>
/// <param name="Verb">Command name when <paramref name="Help"/> is false and parsing succeeded.</param>
/// <param name="BaseRef">Git ref for <c>--base</c>, when set.</param>
/// <param name="Paths">Repeated <c>--path</c> values. Non-empty means they replace configured roots.</param>
/// <param name="JsonPath">Report path for <c>--json</c>, when set.</param>
/// <param name="Threshold">Override for crap, mutate, and mutate-gherkin.</param>
/// <param name="Error">Parse error. The process exits 2.</param>
internal sealed record ParsedArgs(
    bool Help,
    string? Verb,
    string? BaseRef,
    IReadOnlyList<string> Paths,
    string? JsonPath,
    double? Threshold,
    string? Error
)
{
    public static ParsedArgs HelpScreen() => new(true, null, null, [], null, null, null);

    public static ParsedArgs Failed(string error) => new(false, null, null, [], null, null, error);
}
