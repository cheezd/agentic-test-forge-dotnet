using System.Text.RegularExpressions;

namespace AgenticTestForge.Gherkin;

internal static partial class StepInventory
{
    private static readonly string[] Samples = ["1", "1.5", "word", "\"text\""];

    public static IReadOnlyList<Regex> Load(string projectDirectory)
    {
        var patterns = new List<Regex>();
        if (!Directory.Exists(projectDirectory))
        {
            return patterns;
        }

        foreach (
            var file in Directory.EnumerateFiles(
                projectDirectory,
                "*.cs",
                SearchOption.AllDirectories
            )
        )
        {
            if (IsBuildOutput(file))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            foreach (Match match in AttributePattern().Matches(text))
            {
                patterns.Add(ToRegex(Unescape(match.Groups[1].Value)));
            }
        }

        return patterns;
    }

    public static IReadOnlyList<string> Missing(
        IReadOnlyList<string> steps,
        IReadOnlyList<Regex> patterns
    )
    {
        var missing = new List<string>();
        foreach (var step in steps)
        {
            if (!patterns.Any(pattern => Matches(pattern, step)))
            {
                missing.Add(step);
            }
        }

        return missing;
    }

    private static bool Matches(Regex pattern, string step)
    {
        if (pattern.IsMatch(step))
        {
            return true;
        }

        if (!step.Contains('<', StringComparison.Ordinal))
        {
            return false;
        }

        var substituted = PlaceholderPattern().Replace(step, "SAMPLE");
        return Samples.Any(sample =>
            pattern.IsMatch(substituted.Replace("SAMPLE", sample, StringComparison.Ordinal))
        );
    }

    private static Regex ToRegex(string template)
    {
        var pieces = TokenPattern().Split(template);
        var pattern = "^";
        foreach (var piece in pieces)
        {
            if (piece.StartsWith('{') && piece.EndsWith('}'))
            {
                var token = piece[1..^1];
                pattern += token switch
                {
                    "int" or "byte" or "short" or "long" => @"-?\d+",
                    "float" or "double" or "decimal" => @"-?\d+(?:\.\d+)?",
                    "word" => @"\S+",
                    "string" => """(?:"[^"]*"|\S+)""",
                    _ => ".+",
                };
            }
            else
            {
                pattern += Regex.Escape(piece);
            }
        }

        return new Regex(pattern + "$", RegexOptions.CultureInvariant);
    }

    private static string Unescape(string value) =>
        value.Replace("\\\"", "\"", StringComparison.Ordinal);

    private static bool IsBuildOutput(string path) =>
        path.Contains(
            $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
            StringComparison.OrdinalIgnoreCase
        )
        || path.Contains(
            $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
            StringComparison.OrdinalIgnoreCase
        );

    [GeneratedRegex(
        """\[\s*(?:Given|When|Then|Step)\s*\(\s*@?"((?:\\.|[^"])*)"\s*\)\s*\]""",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
    )]
    private static partial Regex AttributePattern();

    [GeneratedRegex(@"(\{[^}]+\})")]
    private static partial Regex TokenPattern();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex PlaceholderPattern();
}
