using AgenticTestForge.Cli;
using AgenticTestForge.Scope;

namespace AgenticTestForge.Config;

internal static class RunPlanResolver
{
    public static ResolveOutcome Resolve(ParsedArgs parsed, string startDirectory) =>
        Resolve(parsed, startDirectory, GitDiff.ChangedFiles);

    public static ResolveOutcome Resolve(
        ParsedArgs parsed,
        string startDirectory,
        Func<string, string, (IReadOnlyList<string>? Files, string? Error)> changedFiles
    )
    {
        if (parsed.Verb is null || !ForgeVerbs.IsKnown(parsed.Verb))
        {
            throw new ArgumentException("Resolve requires a known verb.", nameof(parsed));
        }

        var loaded = ForgeConfigLoader.Load(startDirectory);
        if (loaded.Error is not null || loaded.Config is null || loaded.RepoRoot is null)
        {
            return ResolveOutcome.Failed(loaded.Error ?? "forge.json was not found.");
        }

        var config = loaded.Config;
        var root = loaded.RepoRoot;
        var paths = parsed.Paths.Count > 0 ? parsed.Paths : config.Paths;
        var (crap, mutation, gherkin) = Thresholds(parsed, config);

        string? testProject = null;
        if (RequiresTestProject(parsed.Verb))
        {
            if (config.TestProject is null)
            {
                return ResolveOutcome.Failed("test_project is missing from forge.json.");
            }

            testProject = ResolveFile(root, config.TestProject);
            if (testProject is null)
            {
                return ResolveOutcome.Failed($"test_project '{config.TestProject}' was not found.");
            }
        }

        var skipGherkin = config.AcceptanceProject is null;
        string? acceptanceProject = null;
        if (!skipGherkin && UsesAcceptanceProject(parsed.Verb))
        {
            acceptanceProject = ResolveFile(root, config.AcceptanceProject!);
            if (acceptanceProject is null)
            {
                return ResolveOutcome.Failed(
                    $"acceptance_project '{config.AcceptanceProject}' was not found."
                );
            }
        }

        IReadOnlyList<string>? scopedFiles = null;
        if (parsed.BaseRef is not null)
        {
            var diff = changedFiles(root, parsed.BaseRef);
            if (diff.Error is not null || diff.Files is null)
            {
                return ResolveOutcome.Failed(
                    diff.Error ?? $"git diff failed for base ref '{parsed.BaseRef}'."
                );
            }

            scopedFiles = Filter(diff.Files, paths);
        }

        return ResolveOutcome.Ok(
            new RunPlan(
                parsed.Verb,
                root,
                paths,
                parsed.BaseRef,
                scopedFiles,
                parsed.JsonPath,
                crap,
                mutation,
                gherkin,
                testProject,
                acceptanceProject,
                skipGherkin
            )
        );
    }

    private static bool RequiresTestProject(string verb) =>
        verb is ForgeVerbs.Check or ForgeVerbs.Crap or ForgeVerbs.Mutate;

    private static bool UsesAcceptanceProject(string verb) =>
        verb is ForgeVerbs.Check or ForgeVerbs.MutateGherkin;

    private static (double Crap, double Mutation, double Gherkin) Thresholds(
        ParsedArgs parsed,
        ForgeConfig config
    )
    {
        var crap = config.CrapThreshold;
        var mutation = config.MutationThreshold;
        var gherkin = config.GherkinThreshold;
        if (parsed.Threshold is not double threshold)
        {
            return (crap, mutation, gherkin);
        }

        return parsed.Verb switch
        {
            ForgeVerbs.Crap => (threshold, mutation, gherkin),
            ForgeVerbs.Mutate => (crap, threshold, gherkin),
            ForgeVerbs.MutateGherkin => (crap, mutation, threshold),
            _ => (crap, mutation, gherkin),
        };
    }

    private static string? ResolveFile(string repoRoot, string relative)
    {
        var full = Path.IsPathRooted(relative)
            ? relative
            : Path.GetFullPath(Path.Combine(repoRoot, relative));
        return File.Exists(full) ? full : null;
    }

    private static IReadOnlyList<string> Filter(
        IReadOnlyList<string> files,
        IReadOnlyList<string> roots
    )
    {
        var normalized = files.Select(RepoPaths.Normalize).ToArray();
        if (roots.Count == 0)
        {
            return normalized;
        }

        return normalized.Where(file => roots.Any(root => RepoPaths.IsUnder(file, root))).ToArray();
    }
}
