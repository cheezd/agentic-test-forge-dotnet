using System.Globalization;
using System.Text.Json;
using AgenticTestForge.Execution;

namespace AgenticTestForge.Crap;

internal static class CrapTool
{
    public const string Version = "0.1.1";

    public static (string? Exe, string? Error) EnsureInstalled(string workingDirectory) =>
        PinnedDotnetTool.EnsureInstalled(
            "Crap4DotNet",
            Version,
            "Crap4DotNet",
            ["dotnet-crap.exe", "dotnet-crap"],
            workingDirectory
        );

    public static (IReadOnlyList<CrapToolMethod> Methods, string? Error) Analyze(
        string exe,
        string projectPath,
        string coveragePath,
        double threshold,
        string workingDirectory
    )
    {
        var output = Path.Combine(
            Path.GetTempPath(),
            "crap-" + Guid.NewGuid().ToString("N") + ".json"
        );
        try
        {
            var result = ProcessRunner.Run(
                exe,
                workingDirectory,
                [
                    "analyze",
                    projectPath,
                    "--coverage",
                    coveragePath,
                    "--threshold",
                    threshold.ToString(CultureInfo.InvariantCulture),
                    "--output",
                    output,
                ],
                new Dictionary<string, string> { ["DOTNET_ROLL_FORWARD"] = "Major" }
            );
            if (result.ExitCode < 0)
            {
                return ([], "Crap4DotNet executable was not found.");
            }

            if (!File.Exists(output) || result.ExitCode == 2)
            {
                return ([], PinnedDotnetTool.Detail("Crap4DotNet analysis failed.", result));
            }

            try
            {
                return (CrapToolReport.Parse(File.ReadAllText(output)), null);
            }
            catch (JsonException)
            {
                return ([], "Crap4DotNet analysis failed.");
            }
        }
        finally
        {
            PinnedDotnetTool.TryDeleteFile(output);
        }
    }
}
