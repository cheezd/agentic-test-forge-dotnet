namespace AgenticTestForge.Cli;

internal static class HelpText
{
    public static string Render() =>
        $"""
            forge {ToolVersion.Current}
            C# quality gates for agentic development.

            Usage:
              forge <command> [options]

            Commands:
              {ForgeVerbs.Check, -16} Run the hard gates, then the advisory passes
              {ForgeVerbs.Crap, -16} Score CRAP against the ceiling
              {ForgeVerbs.Mutate, -16} Score mutation against the kill-rate floor
              {ForgeVerbs.MutateGherkin, -16} Score Gherkin mutation against the floor
              {ForgeVerbs.Dry, -16} Advisory duplication pass

            Options:
              --base <ref>       Limit analysis to the git diff against this ref
              --path <path>      Source root. Repeat to replace the configured roots
              --json <file>      Write the JSON report. Human output still prints
              --threshold <n>    Ceiling or floor for crap, mutate, and mutate-gherkin
              -h, --help         Show this help

            """;
}
