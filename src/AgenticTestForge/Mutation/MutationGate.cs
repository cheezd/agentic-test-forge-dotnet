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
            return Passed(plan);
        }

        if (string.IsNullOrWhiteSpace(plan.TestProject))
        {
            return MissingProject(plan);
        }

        var testDirectory = Path.GetDirectoryName(plan.TestProject);
        if (string.IsNullOrEmpty(testDirectory))
        {
            return MissingProject(plan);
        }

        var projectName = ProjectSelector.Select(plan.TestProject!, plan.RepoRoot, plan.Paths);
        if (projectName.Error is not null)
        {
            return ToolError(plan, projectName.Error);
        }

        var tool = StrykerTool.EnsureInstalled(plan.RepoRoot);
        if (tool.Error is not null || tool.Exe is null)
        {
            return ToolError(plan, tool.Error ?? "Stryker.NET 5.0.0 could not be installed.");
        }

        return RunAndScore(plan, tool.Exe, testDirectory, projectName.Name);
    }

    private static MutationEvaluation MissingProject(RunPlan plan) =>
        ToolError(plan, "test_project is missing from forge.json.");

    private static MutationEvaluation ToolError(RunPlan plan, string message) =>
        MutationEvaluation.ToolError(plan.MutationThreshold, message);

    private static MutationEvaluation Passed(RunPlan plan) => Scored(plan, [], []);

    private static MutationEvaluation Scored(
        RunPlan plan,
        IReadOnlyList<StrykerFile> files,
        IReadOnlyList<string>? scopedFiles
    ) =>
        MutationEvaluation.Ok(
            Gate(
                plan,
                MutationScorer.Score(
                    files,
                    plan.MutationThreshold,
                    plan.RepoRoot,
                    plan.Paths,
                    scopedFiles
                )
            )
        );

    private static MutationEvaluation RunAndScore(
        RunPlan plan,
        string exe,
        string testDirectory,
        string? projectName
    )
    {
        var output = Path.Combine(
            Path.GetTempPath(),
            "agentic-test-forge",
            "stryker-" + Guid.NewGuid().ToString("N")
        );
        try
        {
            var run = StrykerTool.Run(
                exe,
                testDirectory,
                output,
                projectName,
                plan.BaseRef,
                MutateGlobs(plan)
            );
            if (run.Error is not null || run.Report is null)
            {
                return ToolError(plan, run.Error ?? "Stryker.NET analysis failed.");
            }

            var files = ReadReport(run.Report);
            if (files.Error is not null)
            {
                return ToolError(plan, files.Error);
            }

            return Scored(plan, files.Files!, plan.ScopedFiles);
        }
        finally
        {
            PinnedDotnetTool.TryDeleteDirectory(output);
        }
    }

    private static (IReadOnlyList<StrykerFile>? Files, string? Error) ReadReport(string path)
    {
        try
        {
            return (StrykerReport.Parse(File.ReadAllText(path)), null);
        }
        catch (System.Text.Json.JsonException)
        {
            return (null, "Stryker.NET analysis failed.");
        }
        catch (IOException)
        {
            return (null, "Stryker.NET analysis failed.");
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
