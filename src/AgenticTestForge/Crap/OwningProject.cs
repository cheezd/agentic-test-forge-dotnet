namespace AgenticTestForge.Crap;

internal static class OwningProject
{
    public static string Resolve(string path, string repoRoot)
    {
        var full = Path.GetFullPath(path);
        if (File.Exists(full) && full.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            return full;
        }

        var directory = Directory.Exists(full) ? full : Path.GetDirectoryName(full);
        var root = Path.GetFullPath(repoRoot);
        while (!string.IsNullOrEmpty(directory))
        {
            var projects = Directory.GetFiles(directory, "*.csproj");
            if (projects.Length == 1)
            {
                return projects[0];
            }

            if (
                projects.Length > 1
                || string.Equals(directory, root, StringComparison.OrdinalIgnoreCase)
            )
            {
                return full;
            }

            var parent = Path.GetDirectoryName(directory);
            if (string.Equals(parent, directory, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            directory = parent;
        }

        return full;
    }
}
