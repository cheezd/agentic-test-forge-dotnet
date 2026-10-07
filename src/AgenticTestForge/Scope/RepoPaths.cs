namespace AgenticTestForge.Scope;

internal static class RepoPaths
{
    public static string Normalize(string path) => path.Replace('\\', '/').TrimStart('/');

    public static bool IsUnder(string relativeFile, string root)
    {
        var prefix = Normalize(root).TrimEnd('/');
        if (prefix.Length == 0)
        {
            return true;
        }

        var file = Normalize(relativeFile);
        return file.Equals(prefix, StringComparison.OrdinalIgnoreCase)
            || file.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsUnderAny(string relativeFile, IReadOnlyList<string> roots) =>
        roots.Count == 0 || roots.Any(root => IsUnder(relativeFile, root));
}
