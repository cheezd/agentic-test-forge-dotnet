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
            return CrapEvaluation.Ok(GateReport.Crap(plan.CrapThreshold, GateStatus.Pass, []));
        }

        if (string.IsNullOrWhiteSpace(plan.TestProject))
        {
            return CrapEvaluation.ToolError(
                plan.CrapThreshold,
                "test_project is missing from forge.json."
            );
        }

        if (!CoverletReference.IsPresent(plan.TestProject, plan.RepoRoot))
        {
            return CrapEvaluation.ToolError(
                plan.CrapThreshold,
                "coverlet.collector is not referenced by the test project."
            );
        }

        var resultsDirectory = Path.Combine(
            Path.GetTempPath(),
            "agentic-test-forge",
            Guid.NewGuid().ToString("N")
        );
        try
        {
            Directory.CreateDirectory(resultsDirectory);
            var test = ProcessRunner.Run(
                "dotnet",
                plan.RepoRoot,
                [
                    "test",
                    plan.TestProject,
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
                return CrapEvaluation.ToolError(
                    plan.CrapThreshold,
                    "dotnet executable was not found."
                );
            }

            var cobertura = FindCobertura(resultsDirectory);
            if (cobertura is null)
            {
                return CrapEvaluation.ToolError(
                    plan.CrapThreshold,
                    PinnedDotnetTool.Detail("coverage could not be produced.", test)
                );
            }

            CoberturaCoverage coverage;
            try
            {
                coverage = CoberturaCoverage.ParseFile(cobertura);
            }
            catch (System.Xml.XmlException)
            {
                return CrapEvaluation.ToolError(
                    plan.CrapThreshold,
                    "coverage could not be produced."
                );
            }
            catch (IOException)
            {
                return CrapEvaluation.ToolError(
                    plan.CrapThreshold,
                    "coverage could not be produced."
                );
            }

            var tool = CrapTool.EnsureInstalled(plan.RepoRoot);
            if (tool.Error is not null || tool.Exe is null)
            {
                return CrapEvaluation.ToolError(
                    plan.CrapThreshold,
                    tool.Error ?? "Crap4DotNet 0.1.1 could not be installed."
                );
            }

            var methods = new List<CrapToolMethod>();
            foreach (var target in AnalysisTargets(plan))
            {
                if (target.Error is not null || target.Path is null)
                {
                    return CrapEvaluation.ToolError(
                        plan.CrapThreshold,
                        target.Error ?? "configured path was not found."
                    );
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
                    return CrapEvaluation.ToolError(plan.CrapThreshold, analyzed.Error);
                }

                methods.AddRange(analyzed.Methods);
            }

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
        finally
        {
            PinnedDotnetTool.TryDeleteDirectory(resultsDirectory);
        }
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
