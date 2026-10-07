using System.Text.Json;
using AgenticTestForge.Reporting;

namespace AgenticTestForge.Dry;

internal static class SonarReport
{
    public static DryCollection Read(SonarPayload payload)
    {
        try
        {
            return new DryCollection(
                true,
                ReadFindings(payload.Issues),
                ReadCondition(payload.QualityGate)
            );
        }
        catch (JsonException)
        {
            return DryCollection.NotRun;
        }
    }

    private static DuplicationsCondition? ReadCondition(string json)
    {
        using var document = JsonDocument.Parse(json);
        var conditions = Conditions(document.RootElement);
        if (conditions is null)
        {
            return null;
        }

        return Preferred(conditions.Value);
    }

    private static JsonElement? Conditions(JsonElement root)
    {
        if (Child(root, "projectStatus") is not JsonElement status)
        {
            return null;
        }

        return Child(status, "conditions");
    }

    private static JsonElement? Child(JsonElement element, string name) =>
        element.TryGetProperty(name, out var property) ? property : null;

    private static DuplicationsCondition? Preferred(JsonElement conditions)
    {
        DuplicationsCondition? first = null;
        foreach (var condition in conditions.EnumerateArray())
        {
            var parsed = Duplication(condition);
            if (parsed is null)
            {
                continue;
            }

            if (IsDensity(parsed.Metric))
            {
                return parsed;
            }

            first ??= parsed;
        }

        return first;
    }

    private static DuplicationsCondition? Duplication(JsonElement condition)
    {
        var metric = Text(condition, "metricKey");
        if (metric is null || !IsDuplicationMetric(metric))
        {
            return null;
        }

        return new DuplicationsCondition(
            metric,
            Text(condition, "status") ?? "",
            Actual(condition)
        );
    }

    private static bool IsDuplicationMetric(string metric) =>
        metric.Contains("duplicat", StringComparison.OrdinalIgnoreCase);

    private static bool IsDensity(string metric) =>
        metric.Equals("duplicated_lines_density", StringComparison.OrdinalIgnoreCase);

    private static string? Actual(JsonElement condition)
    {
        if (!condition.TryGetProperty("actualValue", out var actual))
        {
            return null;
        }

        return actual.ValueKind switch
        {
            JsonValueKind.String => actual.GetString(),
            JsonValueKind.Number => actual.GetRawText(),
            _ => null,
        };
    }

    private static List<DryFinding> ReadFindings(string json)
    {
        using var document = JsonDocument.Parse(json);
        var findings = new List<DryFinding>();
        if (!document.RootElement.TryGetProperty("issues", out var issues))
        {
            return findings;
        }

        foreach (var issue in issues.EnumerateArray())
        {
            var finding = Finding(issue);
            if (finding is not null)
            {
                findings.Add(finding);
            }
        }

        return findings;
    }

    private static DryFinding? Finding(JsonElement issue)
    {
        var rule = Text(issue, "rule");
        var message = Text(issue, "message");
        if (!IsDuplication(rule, message))
        {
            return null;
        }

        var key = Text(issue, "key");
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        return new DryFinding(
            true,
            key,
            Filepath(Text(issue, "component")),
            null,
            null,
            null,
            SonarDrySource.IdValue
        );
    }

    private static bool IsDuplication(string? rule, string? message) =>
        Contains(rule, "duplicat")
        || Contains(rule, "S4144")
        || Contains(message, "duplicat")
        || Contains(message, "identical");

    private static bool Contains(string? text, string value) =>
        text is not null && text.Contains(value, StringComparison.OrdinalIgnoreCase);

    private static string? Filepath(string? component)
    {
        if (string.IsNullOrWhiteSpace(component))
        {
            return null;
        }

        var split = component.IndexOf(':');
        return split < 0 ? component : component[(split + 1)..];
    }

    private static string? Text(JsonElement element, string name)
    {
        if (
            !element.TryGetProperty(name, out var property)
            || property.ValueKind != JsonValueKind.String
        )
        {
            return null;
        }

        return property.GetString();
    }
}
