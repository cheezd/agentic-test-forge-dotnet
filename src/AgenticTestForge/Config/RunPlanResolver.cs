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
        var testProject = ResolveTestProject(parsed.Verb, config, root);
        if (testProject.Error is not null)
        {
            return ResolveOutcome.Failed(testProject.Error);
        }

        var acceptance = ResolveAcceptanceProject(parsed.Verb, config, root);
        if (acceptance.Error is not null)
        {
            return ResolveOutcome.Failed(acceptance.Error);
        }

        var scopedFiles = Scope(parsed, root, paths, changedFiles);
        if (scopedFiles.Error is not null)
        {
            return ResolveOutcome.Failed(scopedFiles.Error);
        }

        return ResolveOutcome.Ok(
            new RunPlan(
                parsed.Verb,
                root,
                paths,
                parsed.BaseRef,
                scopedFiles.Files,
                parsed.JsonPath,
                crap,
                mutation,
                gherkin,
                testProject.Path,
                acceptance.Path,
                acceptance.SkipGherkin
            )
        );
    }

    private static (string? Path, string? Error) ResolveTestProject(
        string verb,
        ForgeConfig config,
        string repoRoot
    )
    {
        if (!RequiresTestProject(verb))
        {
            return (null, null);
        }

        if (config.TestProject is null)
        {
            return (null, "test_project is missing from forge.json.");
        }

        var full = ResolveFile(repoRoot, config.TestProject);
        return full is null
            ? (null, $"test_project '{config.TestProject}' was not found.")
            : (full, null);
    }

    private static (string? Path, string? Error, bool SkipGherkin) ResolveAcceptanceProject(
        string verb,
        ForgeConfig config,
        string repoRoot
    )
    {
        if (config.AcceptanceProject is null)
        {
            return (null, null, true);
        }

        if (!UsesAcceptanceProject(verb))
        {
            return (null, null, false);
        }

        var full = ResolveFile(repoRoot, config.AcceptanceProject);
        return full is null
            ? (null, $"acceptance_project '{config.AcceptanceProject}' was not found.", false)
            : (full, null, false);
    }

    private static (IReadOnlyList<string>? Files, string? Error) Scope(
        ParsedArgs parsed,
        string repoRoot,
        IReadOnlyList<string> paths,
        Func<string, string, (IReadOnlyList<string>? Files, string? Error)> changedFiles
    )
    {
        if (parsed.BaseRef is null)
        {
            return (null, null);
        }

        var diff = changedFiles(repoRoot, parsed.BaseRef);
        if (diff.Error is not null || diff.Files is null)
        {
            return (null, diff.Error ?? $"git diff failed for base ref '{parsed.BaseRef}'.");
        }

        return (Filter(diff.Files, paths), null);
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
