using AgenticTestForge.Config;
using AgenticTestForge.Reporting;
using AgenticTestForge.Scope;

namespace AgenticTestForge.Dry;

internal sealed class JaccardDrySource : IDrySource
{
    public const string IdValue = "jaccard";

    public string Id => IdValue;

    public DryCollection Collect(RunPlan plan)
    {
        var files = SourceFiles(plan);
        if (files.Count == 0)
        {
            return DryCollection.NotRun;
        }

        return new DryCollection(true, Score(plan.RepoRoot, files), null);
    }

    private static IReadOnlyList<DryFinding> Score(string repoRoot, IReadOnlyList<string> files)
    {
        var units = new List<MethodUnit>();
        foreach (var file in files)
        {
            units.AddRange(Read(repoRoot, file));
        }

        return JaccardSimilarity.Score(units);
    }

    private static IReadOnlyList<MethodUnit> Read(string repoRoot, string file)
    {
        try
        {
            return JaccardSimilarity.Read(Relative(repoRoot, file), File.ReadAllText(file));
        }
        catch (IOException)
        {
            return [];
        }
    }

    private static IReadOnlyList<string> SourceFiles(RunPlan plan)
    {
        if (plan.ScopedFiles is not null)
        {
            return plan
                .ScopedFiles.Where(static file =>
                    file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                )
                .Select(file => Absolute(plan.RepoRoot, file))
                .Where(static file => File.Exists(file) && !IsBuildOutput(file))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        var roots =
            plan.Paths.Count == 0
                ? [plan.RepoRoot]
                : plan.Paths.Select(path => Absolute(plan.RepoRoot, path)).ToArray();
        return roots
            .SelectMany(Enumerate)
            .Where(static file => !IsBuildOutput(file))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IEnumerable<string> Enumerate(string root)
    {
        if (File.Exists(root))
        {
            return root.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ? [root] : [];
        }

        if (!Directory.Exists(root))
        {
            return [];
        }

        return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories);
    }

    private static string Absolute(string repoRoot, string path) =>
        Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(repoRoot, path));

    private static string Relative(string repoRoot, string path) =>
        RepoPaths.Normalize(
            Path.GetRelativePath(Path.GetFullPath(repoRoot), Path.GetFullPath(path))
        );

    private static bool IsBuildOutput(string path) =>
        path.Contains(
            $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
            StringComparison.OrdinalIgnoreCase
        )
        || path.Contains(
            $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
            StringComparison.OrdinalIgnoreCase
        );
}
