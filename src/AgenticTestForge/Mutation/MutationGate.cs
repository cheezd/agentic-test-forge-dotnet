using AgenticTestForge.Config;
using AgenticTestForge.Execution;
using AgenticTestForge.Reporting;

namespace AgenticTestForge.Mutation;

internal static class MutationGate
{
    public static MutationEvaluation Evaluate(RunPlan plan)
    {
        if (plan.ScopedFiles is { Count: 0 })
        {
            return MutationEvaluation.Ok(
                Gate(
                    plan,
                    MutationScorer.Score([], plan.MutationThreshold, plan.RepoRoot, plan.Paths, [])
                )
            );
        }

        if (string.IsNullOrWhiteSpace(plan.TestProject))
        {
            return MutationEvaluation.ToolError(
                plan.MutationThreshold,
                "test_project is missing from forge.json."
            );
        }

        var testDirectory = Path.GetDirectoryName(plan.TestProject);
        if (string.IsNullOrEmpty(testDirectory))
        {
            return MutationEvaluation.ToolError(
                plan.MutationThreshold,
                "test_project is missing from forge.json."
            );
        }

        var projectName = ProjectSelector.Select(plan.TestProject!, plan.RepoRoot, plan.Paths);
        if (projectName.Error is not null)
        {
            return MutationEvaluation.ToolError(plan.MutationThreshold, projectName.Error);
        }

        var tool = StrykerTool.EnsureInstalled(plan.RepoRoot);
        if (tool.Error is not null || tool.Exe is null)
        {
            return MutationEvaluation.ToolError(
                plan.MutationThreshold,
                tool.Error ?? "Stryker.NET 5.0.0 could not be installed."
            );
        }

        var output = Path.Combine(
            Path.GetTempPath(),
            "agentic-test-forge",
            "stryker-" + Guid.NewGuid().ToString("N")
        );
        try
        {
            var run = StrykerTool.Run(
                tool.Exe,
                testDirectory,
                output,
                projectName.Name,
                plan.BaseRef,
                MutateGlobs(plan)
            );
            if (run.Error is not null || run.Report is null)
            {
                return MutationEvaluation.ToolError(
                    plan.MutationThreshold,
                    run.Error ?? "Stryker.NET analysis failed."
                );
            }

            IReadOnlyList<StrykerFile> files;
            try
            {
                files = StrykerReport.Parse(File.ReadAllText(run.Report));
            }
            catch (System.Text.Json.JsonException)
            {
                return MutationEvaluation.ToolError(
                    plan.MutationThreshold,
                    "Stryker.NET analysis failed."
                );
            }
            catch (IOException)
            {
                return MutationEvaluation.ToolError(
                    plan.MutationThreshold,
                    "Stryker.NET analysis failed."
                );
            }

            return MutationEvaluation.Ok(
                Gate(
                    plan,
                    MutationScorer.Score(
                        files,
                        plan.MutationThreshold,
                        plan.RepoRoot,
                        plan.Paths,
                        plan.ScopedFiles
                    )
                )
            );
        }
        finally
        {
            PinnedDotnetTool.TryDeleteDirectory(output);
        }
    }

    private static IGateReport Gate(RunPlan plan, MutationScore score) =>
        GateReport.Mutation(
            plan.MutationThreshold,
            score.Status,
            score.Findings,
            score.Survivors,
            score.Inconclusive
        );

    private static IReadOnlyList<string> MutateGlobs(RunPlan plan)
    {
        if (plan.ScopedFiles is null)
        {
            return [];
        }

        return plan
            .ScopedFiles.Where(static file =>
                file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
            )
            .Select(static file => "**/" + Path.GetFileName(file))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
