using AgenticTestForge.Reporting;
using AgenticTestForge.Scope;

namespace AgenticTestForge.Mutation;

internal static class MutationScorer
{
    public static MutationScore Score(
        IReadOnlyList<StrykerFile> files,
        double floor,
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

        var findings = new List<MutationFinding>();
        var survivors = new List<MutantRef>();
        var inconclusive = new List<MutantRef>();
        foreach (var file in files)
        {
            var relative = Relative(file.Filepath, repoRoot);
            if (!RepoPaths.IsUnderAny(relative, roots))
            {
                continue;
            }

            if (scope is not null && !scope.Contains(RepoPaths.Normalize(relative)))
            {
                continue;
            }

            var killed = 0;
            var survived = 0;
            var open = 0;
            foreach (var mutant in file.Mutants)
            {
                var member = MemberLocator.Find(file.Source, mutant.Line);
                var reference = new MutantRef(relative, member, mutant.Mutator);
                if (IsKilled(mutant.Status))
                {
                    killed++;
                }
                else if (IsSurvived(mutant.Status))
                {
                    survived++;
                    survivors.Add(reference);
                }
                else
                {
                    open++;
                    inconclusive.Add(reference);
                }
            }

            var total = killed + survived + open;
            if (total == 0)
            {
                continue;
            }

            var rate = killed * 100d / total;
            findings.Add(
                new MutationFinding(
                    relative,
                    rate,
                    killed,
                    total,
                    ThresholdRules.MutationIsUnderFloor(rate, floor)
                )
            );
        }

        var status =
            findings.Any(static finding => finding.AboveThreshold) || inconclusive.Count > 0
                ? GateStatus.Fail
                : GateStatus.Pass;
        return new MutationScore(status, findings, survivors, inconclusive);
    }

    private static bool IsKilled(string status) =>
        status.Equals("Killed", StringComparison.OrdinalIgnoreCase)
        || status.Equals("CompileError", StringComparison.OrdinalIgnoreCase);

    private static bool IsSurvived(string status) =>
        status.Equals("Survived", StringComparison.OrdinalIgnoreCase);

    private static string Relative(string filePath, string repoRoot)
    {
        var full = Path.IsPathRooted(filePath)
            ? Path.GetFullPath(filePath)
            : Path.GetFullPath(Path.Combine(repoRoot, filePath));
        return RepoPaths.Normalize(Path.GetRelativePath(Path.GetFullPath(repoRoot), full));
    }
}

internal sealed record MutationScore(
    GateStatus Status,
    IReadOnlyList<MutationFinding> Findings,
    IReadOnlyList<MutantRef> Survivors,
    IReadOnlyList<MutantRef> Inconclusive
);
