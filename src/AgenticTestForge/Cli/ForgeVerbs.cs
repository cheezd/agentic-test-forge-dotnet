namespace AgenticTestForge.Cli;

/// <summary>Verb names for the forge command.</summary>
internal static class ForgeVerbs
{
    public const string Check = "check";
    public const string Crap = "crap";
    public const string Mutate = "mutate";
    public const string MutateGherkin = "mutate-gherkin";
    public const string Dry = "dry";

    private static readonly HashSet<string> Names = new(StringComparer.Ordinal)
    {
        Check,
        Crap,
        Mutate,
        MutateGherkin,
        Dry,
    };

    public static bool IsKnown(string verb) => Names.Contains(verb);

    public static bool AcceptsThreshold(string verb) => verb is Crap or Mutate or MutateGherkin;
}
