namespace AgenticTestForge.Execution;

internal static class PinnedDotnetTool
{
    public static (string? Exe, string? Error) EnsureInstalled(
        string packageId,
        string version,
        string displayName,
        IReadOnlyList<string> executables,
        string workingDirectory
    )
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "agentic-test-forge",
            "tools",
            packageId,
            version
        );
        var existing = FindExe(directory, executables);
        if (existing is not null)
        {
            return (existing, null);
        }

        Directory.CreateDirectory(directory);
        var install = ProcessRunner.Run(
            "dotnet",
            workingDirectory,
            [
                "tool",
                "install",
                packageId,
                "--version",
                version,
                "--tool-path",
                directory,
                "--add-source",
                "https://api.nuget.org/v3/index.json",
            ]
        );
        if (install.ExitCode != 0)
        {
            return (null, Detail($"{displayName} {version} could not be installed.", install));
        }

        var exe = FindExe(directory, executables);
        return exe is null
            ? (null, $"{displayName} {version} could not be installed.")
            : (exe, null);
    }

    public static string Detail(string message, ProcessOutput output)
    {
        var extra = string.IsNullOrWhiteSpace(output.Stderr) ? output.Stdout : output.Stderr;
        extra = extra.Trim().Replace('\r', ' ').Replace('\n', ' ');
        if (extra.Length == 0)
        {
            return message;
        }

        if (extra.Length > 300)
        {
            extra = extra[^300..];
        }

        return message + " " + extra;
    }

    public static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    public static void TryDeleteDirectory(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static string? FindExe(string directory, IReadOnlyList<string> executables)
    {
        if (!Directory.Exists(directory))
        {
            return null;
        }

        foreach (var name in executables)
        {
            var candidate = Path.Combine(directory, name);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}
