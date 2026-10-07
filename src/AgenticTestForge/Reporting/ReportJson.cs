using System.Text.Json.Nodes;

namespace AgenticTestForge.Reporting;

internal static class ReportJson
{
    public static JsonObject Open(GateStatus status, double? threshold, bool advisory)
    {
        var node = new JsonObject { ["status"] = status.ToWire() };
        if (threshold is double value)
        {
            node["threshold"] = value;
        }

        if (advisory)
        {
            node["advisory"] = true;
        }

        return node;
    }

    public static JsonArray CrapFindings(IReadOnlyList<CrapFinding> findings)
    {
        var array = new JsonArray();
        foreach (var finding in findings)
        {
            array.Add(
                new JsonObject
                {
                    ["qualified_name"] = finding.QualifiedName,
                    ["filepath"] = finding.Filepath,
                    ["complexity"] = finding.Complexity,
                    ["coverage"] = NullOrNumber(finding.Coverage),
                    ["crap_score"] = NullOrNumber(finding.CrapScore),
                    ["above_threshold"] = finding.AboveThreshold,
                }
            );
        }

        return array;
    }

    public static JsonArray GherkinFindings(IReadOnlyList<GherkinFinding> findings)
    {
        var array = new JsonArray();
        foreach (var finding in findings)
        {
            array.Add(
                new JsonObject
                {
                    ["scenario"] = finding.Scenario,
                    ["filepath"] = finding.Filepath,
                    ["score"] = finding.Score,
                    ["killed"] = finding.Killed,
                    ["total"] = finding.Total,
                    ["above_threshold"] = finding.AboveThreshold,
                }
            );
        }

        return array;
    }

    public static JsonArray MutationFindings(IReadOnlyList<MutationFinding> findings)
    {
        var array = new JsonArray();
        foreach (var finding in findings)
        {
            array.Add(
                new JsonObject
                {
                    ["filepath"] = finding.Filepath,
                    ["score"] = finding.Score,
                    ["killed"] = finding.Killed,
                    ["total"] = finding.Total,
                    ["above_threshold"] = finding.AboveThreshold,
                }
            );
        }

        return array;
    }

    public static JsonArray DryFindings(IReadOnlyList<DryFinding> findings)
    {
        var array = new JsonArray();
        foreach (var finding in findings)
        {
            array.Add(
                new JsonObject
                {
                    ["advisory"] = finding.Advisory,
                    ["sonar_issue_key"] = NullOrString(finding.SonarIssueKey),
                    ["filepath"] = NullOrString(finding.Filepath),
                    ["qualified_name"] = NullOrString(finding.QualifiedName),
                    ["duplicate_filepath"] = NullOrString(finding.DuplicateFilepath),
                    ["duplicate_of"] = NullOrString(finding.DuplicateOf),
                    ["source"] = NullOrString(finding.Source),
                }
            );
        }

        return array;
    }

    public static JsonArray Mutants(IReadOnlyList<MutantRef> mutants)
    {
        var array = new JsonArray();
        foreach (var mutant in mutants)
        {
            array.Add(
                new JsonObject
                {
                    ["filepath"] = mutant.Filepath,
                    ["qualified_name"] = mutant.QualifiedName,
                    ["mutator"] = mutant.Mutator,
                }
            );
        }

        return array;
    }

    public static JsonNode NullOrString(string? value) =>
        value is null ? JsonValue.Create((string?)null)! : JsonValue.Create(value)!;

    public static JsonNode NullOrNumber(double? value) =>
        value is null ? JsonValue.Create((double?)null)! : JsonValue.Create(value.Value)!;
}
