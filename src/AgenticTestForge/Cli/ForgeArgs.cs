using System.Globalization;

namespace AgenticTestForge.Cli;

internal static class ForgeArgs
{
    public static ParsedArgs Parse(IReadOnlyList<string> args)
    {
        if (args.Count == 0 || args.Any(static arg => arg is "-h" or "--help"))
        {
            return ParsedArgs.HelpScreen();
        }

        var verb = args[0];
        if (verb.StartsWith('-'))
        {
            return ParsedArgs.Failed($"Unknown option '{verb}'. A command is required.");
        }

        string? baseRef = null;
        string? jsonPath = null;
        double? threshold = null;
        var paths = new List<string>();

        for (var index = 1; index < args.Count; index++)
        {
            var token = args[index];
            SplitOption(token, out var option, out var inline);
            if (!option.StartsWith('-'))
            {
                return ParsedArgs.Failed($"Unexpected argument '{token}'.");
            }

            if (option is not ("--base" or "--path" or "--json" or "--threshold"))
            {
                return ParsedArgs.Failed($"Unknown option '{option}'.");
            }

            if (!TryReadValue(args, ref index, option, inline, out var value, out var readError))
            {
                return ParsedArgs.Failed(readError);
            }

            var error = option switch
            {
                "--base" => TakeBase(ref baseRef, value),
                "--path" => TakePath(paths, value),
                "--json" => TakeJson(ref jsonPath, value),
                "--threshold" => TakeThreshold(ref threshold, value),
                _ => $"Unknown option '{option}'.",
            };
            if (error is not null)
            {
                return ParsedArgs.Failed(error);
            }
        }

        if (threshold is not null && !ForgeVerbs.AcceptsThreshold(verb))
        {
            return ParsedArgs.Failed("--threshold applies to crap, mutate, and mutate-gherkin.");
        }

        return new ParsedArgs(false, verb, baseRef, paths, jsonPath, threshold, null);
    }

    private static string? TakeBase(ref string? baseRef, string value)
    {
        if (baseRef is not null)
        {
            return "Option '--base' was specified more than once.";
        }

        if (value.Length == 0)
        {
            return "Option '--base' requires a git ref.";
        }

        baseRef = value;
        return null;
    }

    private static string? TakePath(List<string> paths, string value)
    {
        if (value.Length == 0)
        {
            return "Option '--path' requires a path.";
        }

        paths.Add(value);
        return null;
    }

    private static string? TakeJson(ref string? jsonPath, string value)
    {
        if (jsonPath is not null)
        {
            return "Option '--json' was specified more than once.";
        }

        if (value.Length == 0)
        {
            return "Option '--json' requires a file path.";
        }

        jsonPath = value;
        return null;
    }

    private static string? TakeThreshold(ref double? threshold, string value)
    {
        if (threshold is not null)
        {
            return "Option '--threshold' was specified more than once.";
        }

        if (!TryParseThreshold(value, out var parsedThreshold, out var thresholdError))
        {
            return thresholdError;
        }

        threshold = parsedThreshold;
        return null;
    }

    private static void SplitOption(string token, out string option, out string? inline)
    {
        var equals = token.IndexOf('=', StringComparison.Ordinal);
        if (token.StartsWith("--", StringComparison.Ordinal) && equals > 2)
        {
            option = token[..equals];
            inline = token[(equals + 1)..];
            return;
        }

        option = token;
        inline = null;
    }

    private static bool TryReadValue(
        IReadOnlyList<string> args,
        ref int index,
        string option,
        string? inline,
        out string value,
        out string error
    )
    {
        if (inline is not null)
        {
            value = inline;
            error = "";
            return true;
        }

        var next = index + 1;
        if (next >= args.Count || (option != "--threshold" && args[next].StartsWith('-')))
        {
            value = "";
            error = $"Option '{option}' requires a value.";
            return false;
        }

        index = next;
        value = args[next];
        error = "";
        return true;
    }

    private static bool TryParseThreshold(string value, out double threshold, out string error)
    {
        if (
            !double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out threshold)
            || !double.IsFinite(threshold)
            || threshold < 0
        )
        {
            threshold = 0;
            error = $"Option '--threshold' expects a number zero or greater, got '{value}'.";
            return false;
        }

        error = "";
        return true;
    }
}
