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

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            var detail = string.IsNullOrWhiteSpace(stderr) ? "git diff failed." : stderr.Trim();
            return (null, $"git diff failed for base ref '{baseRef}': {detail}");
        }

        var files = stdout.Split(
            ['\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );
        return (files, null);
    }
}
