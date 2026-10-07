using System.Xml.Linq;

namespace AgenticTestForge.Crap;

/// <summary>Line coverage from a Cobertura report, with async and iterator state machines folded onto the source member.</summary>
internal sealed class CoberturaCoverage
{
    private readonly Dictionary<string, Dictionary<int, bool>> _lines;

    private CoberturaCoverage(Dictionary<string, Dictionary<int, bool>> lines) => _lines = lines;

    public static CoberturaCoverage Parse(string xml)
    {
        var document = XDocument.Parse(xml);
        var lines = new Dictionary<string, Dictionary<int, bool>>(StringComparer.Ordinal);
        foreach (var method in document.Descendants("method"))
        {
            var name = method.Attribute("name")?.Value;
            var className = method.Ancestors("class").FirstOrDefault()?.Attribute("name")?.Value;
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(className))
            {
                continue;
            }

            var key = Key(className, name);
            if (!lines.TryGetValue(key, out var hits))
            {
                hits = [];
                lines[key] = hits;
            }

            foreach (var line in method.Descendants("line"))
            {
                if (!int.TryParse(line.Attribute("number")?.Value, out var number))
                {
                    continue;
                }

                var hit = int.TryParse(line.Attribute("hits")?.Value, out var count) && count > 0;
                hits[number] = hits.TryGetValue(number, out var existing) ? existing || hit : hit;
            }
        }

        return new CoberturaCoverage(lines);
    }

    public static CoberturaCoverage ParseFile(string path) => Parse(File.ReadAllText(path));

    public double? Fraction(string className, string methodName)
    {
        var resolved = ResolveClass(className);
        var combined = new Dictionary<int, bool>();
        foreach (var name in MethodNames(resolved, methodName))
        {
            Add(combined, Key(resolved, name));
        }

        var prefix = resolved + "/<" + methodName + ">d__";
        foreach (var entry in _lines)
        {
            if (!entry.Key.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            if (!entry.Key.EndsWith("/MoveNext", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var line in entry.Value)
            {
                combined[line.Key] = combined.TryGetValue(line.Key, out var existing)
                    ? existing || line.Value
                    : line.Value;
            }
        }

        if (combined.Count == 0)
        {
            return null;
        }

        var covered = combined.Values.Count(static hit => hit);
        return (double)covered / combined.Count;
    }

    private string ResolveClass(string className)
    {
        foreach (var candidate in ClassCandidates(className))
        {
            if (HasClass(candidate))
            {
                return candidate;
            }
        }

        return className;
    }

    private bool HasClass(string className) =>
        _lines.Keys.Any(key => key.StartsWith(className + "/", StringComparison.Ordinal));

    private static IEnumerable<string> ClassCandidates(string className)
    {
        yield return className;
        var current = className.Replace('+', '/');
        if (!string.Equals(current, className, StringComparison.Ordinal))
        {
            yield return current;
        }

        while (true)
        {
            var split = current.LastIndexOf('.');
            if (split < 0)
            {
                yield break;
            }

            current = string.Concat(current.AsSpan(0, split), "/", current.AsSpan(split + 1));
            yield return current;
        }
    }

    private static IEnumerable<string> MethodNames(string className, string methodName)
    {
        yield return methodName;
        var split = className.LastIndexOfAny(['/', '+', '.']);
        var simple = split < 0 ? className : className[(split + 1)..];
        if (methodName.Equals(simple, StringComparison.Ordinal))
        {
            yield return ".ctor";
        }
    }

    private void Add(Dictionary<int, bool> combined, string key)
    {
        if (!_lines.TryGetValue(key, out var hits))
        {
            return;
        }

        foreach (var line in hits)
        {
            combined[line.Key] = line.Value;
        }
    }

    private static string Key(string className, string methodName) => className + "/" + methodName;
}
