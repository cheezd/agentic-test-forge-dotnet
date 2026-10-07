using AgenticTestForge.Reporting;
using AgenticTestForge.Scope;

namespace AgenticTestForge.Crap;

internal static class CrapMemberScorer
{
    public static IReadOnlyList<CrapFinding> Score(
        IReadOnlyList<CrapToolMethod> methods,
        CoberturaCoverage coverage,
        double ceiling,
        string repoRoot,
        IReadOnlyList<string> roots,
        IReadOnlyList<string>? scopedFiles
    )
    {
        HashSet<string>? scope = null;
        if (scopedFiles is not null)
        {
            scope = new HashSet<string>(
                scopedFiles.Select(RepoPaths.Normalize),
                StringComparer.OrdinalIgnoreCase
            );
        }

        var findings = new List<CrapFinding>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var method in methods)
        {
            if (IsGenerated(method))
            {
                continue;
            }

            var relative = Relative(method.FilePath, repoRoot);
            if (!RepoPaths.IsUnderAny(relative, roots))
            {
                continue;
            }

            if (scope is not null && !scope.Contains(RepoPaths.Normalize(relative)))
            {
                continue;
            }

            var identity = method.FullName + "|" + relative;
            if (!seen.Add(identity))
            {
                continue;
            }

            var className = string.IsNullOrEmpty(method.Namespace)
                ? method.ClassName
                : method.Namespace + "." + method.ClassName;
            var fraction = coverage.Fraction(className, method.MethodName);
            double? score = fraction is null
                ? null
                : CrapScore.Compute(method.Complexity, fraction.Value);
            findings.Add(
                new CrapFinding(
                    string.IsNullOrEmpty(method.FullName)
                        ? className + "." + method.MethodName
                        : method.FullName,
                    relative,
                    method.Complexity,
                    fraction,
                    score,
                    ThresholdRules.CrapIsAboveCeiling(score, ceiling)
                )
            );
        }

        return findings;
    }

    private static bool IsGenerated(CrapToolMethod method) =>
        method.MethodName.Contains('<', StringComparison.Ordinal)
        || method.ClassName.Contains('<', StringComparison.Ordinal);

    private static string Relative(string filePath, string repoRoot)
    {
        var full = Path.GetFullPath(filePath);
        var relative = Path.GetRelativePath(Path.GetFullPath(repoRoot), full);
        return RepoPaths.Normalize(relative);
    }
}
