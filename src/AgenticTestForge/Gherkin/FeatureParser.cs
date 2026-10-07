using System.Text.RegularExpressions;

namespace AgenticTestForge.Gherkin;

internal static partial class FeatureParser
{
    public static (IReadOnlyList<GherkinScenario>? Scenarios, string? Error) ParseFile(string path)
    {
        string text;
        try
        {
            text = File.ReadAllText(path);
        }
        catch (IOException)
        {
            return (null, $"the feature file '{path}' could not be read.");
        }

        return Parse(text, path);
    }

    public static (IReadOnlyList<GherkinScenario>? Scenarios, string? Error) Parse(
        string text,
        string filepath
    )
    {
        var lines = Lines(text);
        if (!lines.Any(static line => FeaturePattern().IsMatch(line)))
        {
            return (null, $"the feature file '{filepath}' could not be parsed.");
        }

        var scenarios = new List<GherkinScenario>();
        var background = new List<string>();
        var index = 0;
        while (index < lines.Length)
        {
            if (BackgroundPattern().IsMatch(lines[index]))
            {
                index++;
                background.AddRange(ReadSteps(lines, ref index));
                continue;
            }

            if (!ScenarioPattern().IsMatch(lines[index]))
            {
                index++;
                continue;
            }

            scenarios.Add(ReadScenario(lines, filepath, background, ref index));
        }

        return (scenarios, null);
    }

    public static string Apply(string text, ExampleRow row, int columnIndex, string mutated)
    {
        var lines = Lines(text);
        var cells = row.Cells.ToArray();
        cells[columnIndex] = mutated;
        var original = lines[row.LineIndex];
        var indent = original[..(original.Length - original.TrimStart().Length)];
        lines[row.LineIndex] = indent + "| " + string.Join(" | ", cells) + " |";
        return string.Join('\n', lines);
    }

    private static GherkinScenario ReadScenario(
        string[] lines,
        string filepath,
        IReadOnlyList<string> background,
        ref int index
    )
    {
        var scenario = ScenarioPattern().Match(lines[index]);
        var name = scenario.Groups[2].Value.Trim();
        var isOutline = scenario.Groups[1].Value.Contains("Outline", StringComparison.Ordinal);
        var startLine = index + 1;
        index++;
        var steps = ReadSteps(lines, ref index);
        var hasExamples = index < lines.Length && ExamplesPattern().IsMatch(lines[index]);
        ExampleTable? table = null;
        if (hasExamples)
        {
            index++;
            table = ReadTable(lines, ref index);
        }

        return new GherkinScenario(
            name,
            filepath,
            startLine,
            isOutline,
            hasExamples,
            table,
            [.. background, .. steps]
        );
    }

    private static string[] Lines(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');

    private static List<string> ReadSteps(string[] lines, ref int index)
    {
        var steps = new List<string>();
        while (index < lines.Length && !StartsNewBlock(lines[index]))
        {
            var step = StepPattern().Match(lines[index]);
            if (step.Success)
            {
                steps.Add(step.Groups[1].Value.Trim());
            }

            index++;
        }

        return steps;
    }

    private static ExampleTable? ReadTable(string[] lines, ref int index)
    {
        var rows = new List<ExampleRow>();
        while (index < lines.Length && TablePattern().IsMatch(lines[index]))
        {
            rows.Add(new ExampleRow(index, Cells(lines[index])));
            index++;
        }

        if (rows.Count == 0)
        {
            return null;
        }

        return new ExampleTable(rows[0].Cells, rows.Skip(1).ToArray());
    }

    private static bool StartsNewBlock(string line) =>
        ScenarioPattern().IsMatch(line)
        || ExamplesPattern().IsMatch(line)
        || BackgroundPattern().IsMatch(line)
        || FeaturePattern().IsMatch(line);

    private static IReadOnlyList<string> Cells(string line)
    {
        var parts = line.Split('|');
        if (parts.Length < 2)
        {
            return [];
        }

        return parts.Skip(1).Take(parts.Length - 2).Select(static cell => cell.Trim()).ToArray();
    }

    [GeneratedRegex(@"^\s*Feature\s*:", RegexOptions.IgnoreCase)]
    private static partial Regex FeaturePattern();

    [GeneratedRegex(@"^\s*Background\s*:", RegexOptions.IgnoreCase)]
    private static partial Regex BackgroundPattern();

    [GeneratedRegex(@"^\s*Scenario( Outline)?\s*:\s*(.*)$", RegexOptions.IgnoreCase)]
    private static partial Regex ScenarioPattern();

    [GeneratedRegex(@"^\s*(?:Examples|Scenarios)\s*:", RegexOptions.IgnoreCase)]
    private static partial Regex ExamplesPattern();

    [GeneratedRegex(@"^\s*(?:Given|When|Then|And|But)\s+(.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex StepPattern();

    [GeneratedRegex(@"^\s*\|")]
    private static partial Regex TablePattern();
}
