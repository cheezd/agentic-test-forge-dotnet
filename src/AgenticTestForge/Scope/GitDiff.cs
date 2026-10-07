using System.ComponentModel;
using System.Diagnostics;

namespace AgenticTestForge.Scope;

internal static class GitDiff
{
    public static (IReadOnlyList<string>? Files, string? Error) ChangedFiles(
        string repoRoot,
        string baseRef
    )
    {
        if (string.IsNullOrWhiteSpace(baseRef) || baseRef.IndexOfAny(['\r', '\n']) >= 0)
        {
            return (null, $"git diff failed for base ref '{baseRef}'.");
        }

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        process.StartInfo.ArgumentList.Add("diff");
        process.StartInfo.ArgumentList.Add("--name-only");
        process.StartInfo.ArgumentList.Add($"{baseRef}...HEAD");

        try
        {
            process.Start();
        }
        catch (Win32Exception)
        {
            return (null, "git executable was not found.");
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        var stdout = stdoutTask.GetAwaiter().GetResult();
        var stderr = stderrTask.GetAwaiter().GetResult();
        if (process.ExitCode != 0)
        {
            var detail = FirstLine(stderr);
            if (detail.Length == 0)
            {
                detail = "git diff failed.";
            }

            return (null, $"git diff failed for base ref '{baseRef}': {detail}");
        }

        var files = stdout.Split(
            ['\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );
        return (files, null);
    }

    private static string FirstLine(string text)
    {
        foreach (var line in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (trimmed.Length > 0)
            {
                return trimmed;
            }
        }

        return "";
    }
}
