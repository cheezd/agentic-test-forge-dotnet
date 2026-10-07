using AgenticTestForge.Execution;

namespace AgenticTestForge.Dry;

internal static class SonarProcess
{
    private static readonly string[] ScannerNames =
    [
        "sonar-scanner",
        "sonar-scanner.exe",
        "sonar-scanner.bat",
        "sonar-scanner.cmd",
    ];

    public static bool IsAvailable(string workingDirectory) => IsAvailable(workingDirectory, Run);

    public static bool IsAvailable(
        string workingDirectory,
        Func<string, string, IReadOnlyList<string>, ProcessOutput> run
    )
    {
        foreach (var name in ScannerNames)
        {
            if (run(name, workingDirectory, ["--version"]).ExitCode == 0)
            {
                return true;
            }
        }

        return false;
    }

    public static SonarPayload? Fetch(SonarSettings settings, string workingDirectory) =>
        Fetch(settings, workingDirectory, Run);

    private static ProcessOutput Run(
        string fileName,
        string workingDirectory,
        IReadOnlyList<string> arguments
    ) => ProcessRunner.Run(fileName, workingDirectory, arguments);

    public static SonarPayload? Fetch(
        SonarSettings settings,
        string workingDirectory,
        Func<string, string, IReadOnlyList<string>, ProcessOutput> run
    )
    {
        var quality = Get(settings, workingDirectory, QualityGatePath(settings), run);
        var issues = Get(settings, workingDirectory, IssuesPath(settings), run);
        return quality is null || issues is null ? null : new SonarPayload(quality, issues);
    }

    private static string QualityGatePath(SonarSettings settings) =>
        "/api/qualitygates/project_status?projectKey=" + Uri.EscapeDataString(settings.ProjectKey);

    private static string IssuesPath(SonarSettings settings) =>
        "/api/issues/search?componentKeys="
        + Uri.EscapeDataString(settings.ProjectKey)
        + "&ps=500&statuses=OPEN,CONFIRMED";

    private static string? Get(
        SonarSettings settings,
        string workingDirectory,
        string path,
        Func<string, string, IReadOnlyList<string>, ProcessOutput> run
    )
    {
        var output = run(
            "curl",
            workingDirectory,
            ["-sS", "-f", "-u", settings.Token + ":", settings.Host + path]
        );
        return output.ExitCode == 0 ? output.Stdout : null;
    }
}
