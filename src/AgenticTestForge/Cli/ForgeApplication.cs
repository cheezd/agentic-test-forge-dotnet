using AgenticTestForge.Exit;
using AgenticTestForge.Reporting;

namespace AgenticTestForge.Cli;

internal static class ForgeApplication
{
    public static int Run(
        IReadOnlyList<string> args,
        TextWriter stdout,
        TextWriter stderr,
        IGateRunner runner
    )
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(stdout);
        ArgumentNullException.ThrowIfNull(stderr);
        ArgumentNullException.ThrowIfNull(runner);

        var parsed = ForgeArgs.Parse(args);
        if (parsed.Help)
        {
            stdout.Write(HelpText.Render());
            return ExitCodes.Pass;
        }

        if (parsed.Error is not null)
        {
            stderr.WriteLine(parsed.Error);
            return ExitCodes.Error;
        }

        if (parsed.Verb is null || !ForgeVerbs.IsKnown(parsed.Verb))
        {
            stderr.WriteLine($"Unknown command '{parsed.Verb}'.");
            stdout.Write(HelpText.Render());
            return ExitCodes.Error;
        }

        var result = runner.Run(parsed);
        if (parsed.JsonPath is not null && result.Report is not null)
        {
            var writeError = TryWriteReport(parsed.JsonPath, result.Report);
            if (writeError is not null)
            {
                var errors = result.Errors.Concat([writeError]).ToArray();
                result = RunResult.From([], toolError: true, errors: errors, report: result.Report);
            }
        }

        foreach (var error in result.Errors)
        {
            stderr.WriteLine(error);
        }

        var status = result.Errors.Count > 0 ? ReportStatus.Error : result.Status;
        stdout.WriteLine($"{parsed.Verb}: {status.ToWire()}");
        return ExitCodes.For(status);
    }

    private static string? TryWriteReport(string path, CheckReport report)
    {
        try
        {
            var destination = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(destination);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(destination, report.ToJson());
            return null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return $"Could not write the JSON report to '{path}': {exception.Message}";
        }
    }
}
