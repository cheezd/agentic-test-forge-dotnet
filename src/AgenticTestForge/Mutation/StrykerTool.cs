using AgenticTestForge.Execution;

namespace AgenticTestForge.Mutation;

internal static class StrykerTool
{
    public const string Version = "5.0.0";

    public static (string? Exe, string? Error) EnsureInstalled(string workingDirectory) =>
        PinnedDotnetTool.EnsureInstalled(
            "dotnet-stryker",
            Version,
            "Stryker.NET",
            ["dotnet-stryker.exe", "dotnet-stryker"],
            workingDirectory
        );

    public static (string? Report, string? Error) Run(
        string exe,
        string testProjectDirectory,
        string outputDirectory,
        string? projectName,
        string? baseRef,
        IReadOnlyList<string> mutateGlobs
    )
    {
        var configPath = Path.Combine(outputDirectory, "stryker-config.json");
        Directory.CreateDirectory(outputDirectory);
        File.WriteAllText(
            configPath,
            """
            {
              "stryker-config": {
                "reporters": ["json"],
                "report-file-name": "mutation-report",
                "thresholds": { "high": 100, "low": 0, "break": 0 }
              }
            }
            """
        );

        var arguments = new List<string>
        {
            "--config-file",
            configPath,
            "--output",
            outputDirectory,
        };
        if (!string.IsNullOrWhiteSpace(projectName))
        {
            arguments.Add("--project");
            arguments.Add(projectName);
        }

        if (!string.IsNullOrWhiteSpace(baseRef))
        {
            arguments.Add("--since:" + baseRef);
        }

        foreach (var glob in mutateGlobs)
        {
            arguments.Add("--mutate");
            arguments.Add(glob);
        }

        var result = ProcessRunner.Run(exe, testProjectDirectory, arguments);
        if (result.ExitCode < 0)
        {
            return (null, "Stryker.NET executable was not found.");
        }

        var report = FindReport(outputDirectory);
        if (report is null)
        {
            return (null, PinnedDotnetTool.Detail("Stryker.NET analysis failed.", result));
        }

        return (report, null);
    }

    private static string? FindReport(string outputDirectory)
    {
        if (!Directory.Exists(outputDirectory))
        {
            return null;
        }

        return Directory
            .EnumerateFiles(outputDirectory, "mutation-report.json", SearchOption.AllDirectories)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }
}
