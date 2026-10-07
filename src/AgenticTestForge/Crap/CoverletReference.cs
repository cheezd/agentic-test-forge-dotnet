namespace AgenticTestForge.Crap;

internal static class CoverletReference
{
    public static bool IsPresent(string testProjectPath, string repoRoot)
    {
        foreach (var file in FilesToSearch(testProjectPath, repoRoot))
        {
            if (!File.Exists(file))
            {
                continue;
            }

            if (
                File.ReadAllText(file)
                    .Contains("coverlet.collector", StringComparison.OrdinalIgnoreCase)
            )
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> FilesToSearch(string testProjectPath, string repoRoot)
    {
        yield return testProjectPath;
        var directory = Path.GetDirectoryName(Path.GetFullPath(testProjectPath));
        var root = Path.GetFullPath(repoRoot);
        while (!string.IsNullOrEmpty(directory))
        {
            yield return Path.Combine(directory, "Directory.Build.props");
            yield return Path.Combine(directory, "Directory.Packages.props");
            if (string.Equals(directory, root, StringComparison.OrdinalIgnoreCase))
            {
                yield break;
            }

            var parent = Path.GetDirectoryName(directory);
            if (string.Equals(parent, directory, StringComparison.OrdinalIgnoreCase))
            {
                yield break;
            }

            directory = parent;
        }
    }
}
