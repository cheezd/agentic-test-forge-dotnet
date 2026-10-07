using AgenticTestForge.Config;
using AgenticTestForge.Execution;
using AgenticTestForge.Reporting;

namespace AgenticTestForge.Crap;

internal static class CrapGate
{
    public static CrapEvaluation Evaluate(RunPlan plan)
    {
        if (plan.ScopedFiles is { Count: 0 })
        {
            return Passed(plan);
        }

        if (string.IsNullOrWhiteSpace(plan.TestProject))
        {
            return MissingProject(plan);
        }

        if (!CoverletReference.IsPresent(plan.TestProject, plan.RepoRoot))
        {
            return MissingCoverlet(plan);
        }

        var collected = Collect(plan);
        if (collected.Error is not null)
        {
            return ToolError(plan, collected.Error);
        }

        return Report(plan, collected.Methods!, collected.Coverage!);
    }

    private static CrapEvaluation Passed(RunPlan plan) =>
        CrapEvaluation.Ok(GateReport.Crap(plan.CrapThreshold, GateStatus.Pass, []));

    private static CrapEvaluation MissingProject(RunPlan plan) =>
        ToolError(plan, "test_project is missing from forge.json.");

    private static CrapEvaluation MissingCoverlet(RunPlan plan) =>
        ToolError(plan, "coverlet.collector is not referenced by the test project.");

    private static CrapEvaluation ToolError(RunPlan plan, string message) =>
        CrapEvaluation.ToolError(plan.CrapThreshold, message);

    private static (
        List<CrapToolMethod>? Methods,
        CoberturaCoverage? Coverage,
        string? Error
    ) Collect(RunPlan plan)
    {
        var resultsDirectory = Path.Combine(
            Path.GetTempPath(),
            "agentic-test-forge",
            Guid.NewGuid().ToString("N")
        );
        try
        {
            var cobertura = RunCoverage(plan, resultsDirectory);
            if (cobertura.Error is not null)
            {
                return (null, null, cobertura.Error);
            }

            var coverage = ReadCoverage(cobertura.Path!);
            if (coverage.Error is not null)
            {
                return (null, null, coverage.Error);
            }

            var methods = Analyze(plan, cobertura.Path!);
            return methods.Error is null
                ? (methods.Methods, coverage.Coverage, null)
                : (null, null, methods.Error);
        }
        finally
        {
            PinnedDotnetTool.TryDeleteDirectory(resultsDirectory);
        }
    }

    private static (string? Path, string? Error) RunCoverage(RunPlan plan, string resultsDirectory)
    {
        Directory.CreateDirectory(resultsDirectory);
        var test = ProcessRunner.Run(
            "dotnet",
            plan.RepoRoot,
            [
                "test",
                plan.TestProject!,
                "--collect:XPlat Code Coverage",
                "--results-directory",
                resultsDirectory,
                "--verbosity",
                "quiet",
                "--",
                "DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura",
            ]
        );
        if (test.ExitCode < 0)
        {
            return (null, "dotnet executable was not found.");
        }

        var cobertura = FindCobertura(resultsDirectory);
        return cobertura is null
            ? (null, PinnedDotnetTool.Detail("coverage could not be produced.", test))
            : (cobertura, null);
    }

    private static (CoberturaCoverage? Coverage, string? Error) ReadCoverage(string path)
    {
        try
        {
            return (CoberturaCoverage.ParseFile(path), null);
        }
        catch (System.Xml.XmlException)
        {
            return (null, "coverage could not be produced.");
        }
        catch (IOException)
        {
            return (null, "coverage could not be produced.");
        }
    }

    private static (List<CrapToolMethod>? Methods, string? Error) Analyze(
        RunPlan plan,
        string cobertura
    )
    {
        var tool = CrapTool.EnsureInstalled(plan.RepoRoot);
        if (tool.Error is not null || tool.Exe is null)
        {
            return (null, tool.Error ?? "Crap4DotNet 0.1.1 could not be installed.");
        }

        var methods = new List<CrapToolMethod>();
        foreach (var target in AnalysisTargets(plan))
        {
            if (target.Error is not null || target.Path is null)
            {
                return (null, target.Error ?? "configured path was not found.");
            }

            var analyzed = CrapTool.Analyze(
                tool.Exe,
                target.Path,
                cobertura,
                plan.CrapThreshold,
                plan.RepoRoot
            );
            if (analyzed.Error is not null)
            {
                return (null, analyzed.Error);
            }

            methods.AddRange(analyzed.Methods);
        }

        return (methods, null);
    }

    private static CrapEvaluation Report(
        RunPlan plan,
        IReadOnlyList<CrapToolMethod> methods,
        CoberturaCoverage coverage
    )
    {
        var findings = CrapMemberScorer.Score(
            methods,
            coverage,
            plan.CrapThreshold,
            plan.RepoRoot,
            plan.Paths,
            plan.ScopedFiles
        );
        var status = findings.Any(static finding => finding.AboveThreshold)
            ? GateStatus.Fail
            : GateStatus.Pass;
        return CrapEvaluation.Ok(GateReport.Crap(plan.CrapThreshold, status, findings));
    }

    private static IEnumerable<(string? Path, string? Error)> AnalysisTargets(RunPlan plan)
    {
        if (plan.Paths.Count == 0)
        {
            yield return (plan.RepoRoot, null);
            yield break;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in plan.Paths)
        {
            var full = Path.IsPathRooted(path)
                ? Path.GetFullPath(path)
                : Path.GetFullPath(Path.Combine(plan.RepoRoot, path));
            if (!File.Exists(full) && !Directory.Exists(full))
            {
                yield return (null, $"configured path '{path}' was not found.");
                yield break;
            }

            var target = OwningProject.Resolve(full, plan.RepoRoot);
            if (seen.Add(target))
            {
                yield return (target, null);
            }
        }
    }

    private static string? FindCobertura(string resultsDirectory)
    {
        if (!Directory.Exists(resultsDirectory))
        {
            return null;
        }

        return Directory
            .EnumerateFiles(resultsDirectory, "coverage.cobertura.xml", SearchOption.AllDirectories)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }
}
