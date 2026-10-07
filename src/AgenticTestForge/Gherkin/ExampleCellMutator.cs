namespace AgenticTestForge.Gherkin;

internal static class ExampleCellMutator
{
    public static IReadOnlyList<string> Mutate(string value)
    {
        var stripped = value.Trim();
        if (stripped.Length == 0)
        {
            return value.Length == 0 ? [""] : MutateString(value);
        }

        var candidates = IsNumber(stripped) ? MutateNumber(stripped) : MutateString(stripped);
        var unique = new List<string>();
        foreach (var candidate in candidates)
        {
            if (candidate != stripped && !unique.Contains(candidate))
            {
                unique.Add(candidate);
            }
        }

        return unique;
    }

    public static IReadOnlyList<ExampleMutation> ForScenario(GherkinScenario scenario)
    {
        if (scenario.Examples is null)
        {
            return [];
        }

        var mutations = new List<ExampleMutation>();
        for (var rowIndex = 0; rowIndex < scenario.Examples.Rows.Count; rowIndex++)
        {
            var row = scenario.Examples.Rows[rowIndex];
            for (var column = 0; column < row.Cells.Count; column++)
            {
                foreach (var mutated in Mutate(row.Cells[column]))
                {
                    mutations.Add(
                        new ExampleMutation(rowIndex, column, row.Cells[column], mutated, row)
                    );
                }
            }
        }

        return mutations;
    }

    private static List<string> MutateString(string value)
    {
        var mutations = new List<string>();
        if (value.Length > 0)
        {
            mutations.Add("");
            mutations.Add(value + "_mutated");
        }
        else
        {
            mutations.Add("mutated");
        }

        if (value.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            mutations.Add("false");
        }
        else if (value.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            mutations.Add("true");
        }

        return mutations;
    }

    private static List<string> MutateNumber(string value)
    {
        if (value.Contains('.', StringComparison.Ordinal))
        {
            if (
                !double.TryParse(
                    value,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var number
                )
            )
            {
                return MutateString(value);
            }

            return
            [
                (number + 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
                (number - 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
                "0",
            ];
        }

        if (
            !int.TryParse(
                value,
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out var integer
            )
        )
        {
            return MutateString(value);
        }

        return [(integer + 1).ToString(), (integer - 1).ToString(), "0"];
    }

    private static bool IsNumber(string value)
    {
        var dots = value.Count(static character => character == '.');
        if (dots > 1)
        {
            return false;
        }

        var digits = dots == 0 ? value : value.Replace(".", "", StringComparison.Ordinal);
        return digits.Length > 0 && digits.All(static character => char.IsDigit(character));
    }
}
