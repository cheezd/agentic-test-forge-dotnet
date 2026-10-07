using AgenticTestForge.Config;
using AgenticTestForge.Execution;
using AgenticTestForge.Reporting;
using AgenticTestForge.Scope;

namespace AgenticTestForge.Gherkin;

internal static class GherkinGate
{
    public static GherkinEvaluation Evaluate(RunPlan plan) => Evaluate(plan, RunProject);

    internal static GherkinEvaluation Evaluate(
        RunPlan plan,
        Func<string, string, string?, int> runTests
    )
    {
        if (string.IsNullOrWhiteSpace(plan.AcceptanceProject))
        {
            return Skipped(plan);
        }

        var projectDirectory = Path.GetDirectoryName(plan.AcceptanceProject);
        if (string.IsNullOrEmpty(projectDirectory))
        {
            return MissingProject(plan);
        }

        var features = FeatureFiles(plan, projectDirectory);
        if (features.Error is not null)
        {
            return ToolError(plan, features.Error);
        }

        if (features.Files.Count == 0)
        {
            return Passed(plan);
        }

        var parsed = ParseFeatures(features.Files);
        if (parsed.Error is not null || parsed.Scenarios is null)
        {
            return ToolError(plan, parsed.Error!);
        }

        var preflight = Preflight(parsed.Scenarios, projectDirectory, plan.RepoRoot);
        if (preflight.Count > 0)
        {
            return Inconclusive(plan, preflight);
        }

        if (!HasMutations(parsed.Scenarios))
        {
            return Passed(plan);
        }

        var baseline = RunBaseline(plan, runTests);
        return baseline ?? Score(plan, parsed.Scenarios, runTests);
    }

    private static GherkinEvaluation Skipped(RunPlan plan) =>
        GherkinEvaluation.Ok(GateReport.Gherkin(plan.GherkinThreshold, GateStatus.Skipped));

    private static GherkinEvaluation MissingProject(RunPlan plan) =>
        ToolError(plan, "acceptance_project is missing from forge.json.");

    private static GherkinEvaluation ToolError(RunPlan plan, string message) =>
        GherkinEvaluation.ToolError(plan.GherkinThreshold, message);

    private static GherkinEvaluation Inconclusive(RunPlan plan, IReadOnlyList<MutantRef> issues) =>
        GherkinEvaluation.Ok(
            GateReport.Gherkin(plan.GherkinThreshold, GateStatus.Fail, [], [], issues)
        );

    private static bool HasMutations(IReadOnlyList<GherkinScenario> scenarios) =>
        scenarios.Any(static scenario => ExampleCellMutator.ForScenario(scenario).Count > 0);

    private static GherkinEvaluation Passed(RunPlan plan) =>
        GherkinEvaluation.Ok(
            GateReport.Gherkin(plan.GherkinThreshold, GateStatus.Pass, [], [], [])
        );

    private static (IReadOnlyList<GherkinScenario>? Scenarios, string? Error) ParseFeatures(
        IReadOnlyList<string> files
    )
    {
        var parsed = new List<GherkinScenario>();
        foreach (var feature in files)
        {
            var document = FeatureParser.ParseFile(feature);
            if (document.Error is not null || document.Scenarios is null)
            {
                return (null, document.Error ?? "the feature file could not be parsed.");
            }

            parsed.AddRange(document.Scenarios);
        }

        return (parsed, null);
    }

    private static GherkinEvaluation? RunBaseline(
        RunPlan plan,
        Func<string, string, string?, int> runTests
    )
    {
        var baseline = runTests(plan.AcceptanceProject!, plan.RepoRoot, null);
        if (baseline < 0)
        {
            return GherkinEvaluation.ToolError(
                plan.GherkinThreshold,
                "dotnet executable was not found."
            );
        }

        if (baseline != 0)
        {
            return GherkinEvaluation.ToolError(
                plan.GherkinThreshold,
                "the acceptance project did not pass before mutation."
            );
        }

        return null;
    }

    private static GherkinEvaluation Score(
        RunPlan plan,
        IReadOnlyList<GherkinScenario> scenarios,
        Func<string, string, string?, int> runTests
    )
    {
        var findings = new List<GherkinFinding>();
        var survivors = new List<MutantRef>();
        foreach (var scenario in scenarios)
        {
            var outcome = RunScenario(plan, scenario, runTests);
            if (outcome.Error is not null)
            {
                return outcome.Error;
            }

            if (outcome.Finding is null)
            {
                continue;
            }

            findings.Add(outcome.Finding);
            survivors.AddRange(outcome.Survivors);
        }

        var status = findings.Any(static finding => finding.AboveThreshold)
            ? GateStatus.Fail
            : GateStatus.Pass;
        return GherkinEvaluation.Ok(
            GateReport.Gherkin(plan.GherkinThreshold, status, findings, survivors, [])
        );
    }

    private static (
        GherkinFinding? Finding,
        IReadOnlyList<MutantRef> Survivors,
        GherkinEvaluation? Error
    ) RunScenario(
        RunPlan plan,
        GherkinScenario scenario,
        Func<string, string, string?, int> runTests
    )
    {
        var mutations = ExampleCellMutator.ForScenario(scenario);
        if (mutations.Count == 0)
        {
            return (null, [], null);
        }

        var original = File.ReadAllText(scenario.Filepath);
        var killed = 0;
        var survivors = new List<MutantRef>();
        try
        {
            foreach (var mutation in mutations)
            {
                var exit = ApplyAndRun(plan, scenario, mutation, original, runTests);
                if (exit < 0)
                {
                    return (
                        null,
                        [],
                        GherkinEvaluation.ToolError(
                            plan.GherkinThreshold,
                            "dotnet executable was not found."
                        )
                    );
                }

                if (exit == 0)
                {
                    survivors.Add(Survivor(plan, scenario, mutation));
                }
                else
                {
                    killed++;
                }
            }
        }
        finally
        {
            File.WriteAllText(scenario.Filepath, original);
        }

        var total = mutations.Count;
        var rate = killed * 100d / total;
        return (
            new GherkinFinding(
                scenario.Name,
                Relative(scenario.Filepath, plan.RepoRoot),
                rate,
                killed,
                total,
                ThresholdRules.MutationIsUnderFloor(rate, plan.GherkinThreshold)
            ),
            survivors,
            null
        );
    }

    private static int ApplyAndRun(
        RunPlan plan,
        GherkinScenario scenario,
        ExampleMutation mutation,
        string original,
        Func<string, string, string?, int> runTests
    )
    {
        File.WriteAllText(
            scenario.Filepath,
            FeatureParser.Apply(original, mutation.Row, mutation.ColumnIndex, mutation.Mutated)
        );
        return runTests(plan.AcceptanceProject!, plan.RepoRoot, scenario.Name);
    }

    private static MutantRef Survivor(
        RunPlan plan,
        GherkinScenario scenario,
        ExampleMutation mutation
    ) =>
        new(
            Relative(scenario.Filepath, plan.RepoRoot),
            scenario.Name,
            mutation.Original + " -> " + mutation.Mutated
        );

    private static IReadOnlyList<MutantRef> Preflight(
        IReadOnlyList<GherkinScenario> scenarios,
        string projectDirectory,
        string repoRoot
    )
    {
        var issues = new List<MutantRef>();
        var names = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var scenario in scenarios)
        {
            var key = scenario.Filepath + "|" + scenario.Name;
            names[key] = names.GetValueOrDefault(key) + 1;
        }

        var patterns = StepInventory.Load(projectDirectory);
        foreach (var scenario in scenarios)
        {
            var relative = Relative(scenario.Filepath, repoRoot);
            if (names[scenario.Filepath + "|" + scenario.Name] > 1)
            {
                issues.Add(new MutantRef(relative, scenario.Name, "duplicate scenario name"));
            }

            if (
                scenario.IsOutline
                && (scenario.Examples is null || scenario.Examples.Rows.Count == 0)
            )
            {
                issues.Add(
                    new MutantRef(relative, scenario.Name, "scenario outline has no examples")
                );
            }

            foreach (var step in StepInventory.Missing(scenario.Steps, patterns))
            {
                issues.Add(new MutantRef(relative, scenario.Name, "Undefined step: " + step));
            }
        }

        return issues;
    }

    private static (IReadOnlyList<string> Files, string? Error) FeatureFiles(
        RunPlan plan,
        string projectDirectory
    )
    {
        var discovered = Directory
            .EnumerateFiles(projectDirectory, "*.feature", SearchOption.AllDirectories)
            .Where(static path => !IsBuildOutput(path))
            .ToArray();
        if (plan.BaseRef is null)
        {
            return (discovered, null);
        }

        var diff = GitDiff.ChangedFiles(plan.RepoRoot, plan.BaseRef);
        if (diff.Error is not null || diff.Files is null)
        {
            return ([], diff.Error ?? "git diff failed.");
        }

        var changed = new HashSet<string>(
            diff.Files.Select(RepoPaths.Normalize),
            StringComparer.OrdinalIgnoreCase
        );
        var selected = discovered
            .Where(path => changed.Contains(Relative(path, plan.RepoRoot)))
            .ToArray();
        return (selected, null);
    }

    private static int RunProject(string project, string workingDirectory, string? scenario)
    {
        var arguments = new List<string> { "test", project, "--nologo", "--verbosity", "quiet" };
        if (!string.IsNullOrWhiteSpace(scenario))
        {
            arguments.Add("--filter");
            arguments.Add("DisplayName~" + scenario);
        }

        return ProcessRunner.Run("dotnet", workingDirectory, arguments).ExitCode;
    }

    private static string Relative(string path, string repoRoot) =>
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
