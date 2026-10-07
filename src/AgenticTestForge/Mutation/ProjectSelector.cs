using System.Xml.Linq;
using AgenticTestForge.Crap;

namespace AgenticTestForge.Mutation;

internal static class ProjectSelector
{
    public static (string? Name, string? Error) Select(
        string testProjectPath,
        string repoRoot,
        IReadOnlyList<string> paths
    )
    {
        IReadOnlyList<string> names;
        try
        {
            names = ProjectReferences(testProjectPath);
        }
        catch (System.Xml.XmlException)
        {
            return (null, "the test project could not be read.");
        }
        catch (IOException)
        {
            return (null, "the test project could not be read.");
        }

        if (names.Count <= 1)
        {
            return (names.Count == 0 ? null : names[0], null);
        }

        var owned = new List<string>();
        foreach (var path in paths)
        {
            var full = Path.IsPathRooted(path)
                ? Path.GetFullPath(path)
                : Path.GetFullPath(Path.Combine(repoRoot, path));
            if (!File.Exists(full) && !Directory.Exists(full))
            {
                continue;
            }

            var project = Path.GetFileName(OwningProject.Resolve(full, repoRoot));
            if (
                names.Contains(project, StringComparer.OrdinalIgnoreCase)
                && !owned.Contains(project)
            )
            {
                owned.Add(project);
            }
        }

        if (owned.Count == 1)
        {
            return (owned[0], null);
        }

        return (null, "the test project references more than one project.");
    }

    private static IReadOnlyList<string> ProjectReferences(string testProjectPath)
    {
        var document = XDocument.Load(testProjectPath);
        return document
            .Descendants("ProjectReference")
            .Select(reference => reference.Attribute("Include")?.Value)
            .Where(static include => !string.IsNullOrWhiteSpace(include))
            .Select(static include => Path.GetFileName(include!))
            .ToArray();
    }
}
