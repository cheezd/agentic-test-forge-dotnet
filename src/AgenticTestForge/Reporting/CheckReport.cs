using System.Text.Json;
using System.Text.Json.Nodes;
using AgenticTestForge.Cli;

namespace AgenticTestForge.Reporting;

/// <summary>JSON envelope for <c>forge check</c> and the single-gate verbs.</summary>
internal sealed record CheckReport(
    string Tool,
    string Version,
    ReportStatus Status,
    string Summary,
    string? Base,
    IReadOnlyList<string> Paths,
    IReadOnlyList<IGateReport> Gates,
    IReadOnlyList<string> Errors
)
{
    public string ToJson()
    {
        var document = new JsonObject
        {
            ["tool"] = Tool,
            ["version"] = Version,
            ["status"] = Status.ToWire(),
            ["summary"] = Summary,
            ["scope"] = new JsonObject
            {
                ["base"] = ReportJson.NullOrString(Base),
                ["paths"] = Strings(Paths),
            },
            ["gates_run"] = Strings(Gates.Select(static gate => gate.Name).ToArray()),
            ["gate_policies"] = Policies(),
            ["errors"] = Strings(Errors),
            ["reports"] = Reports(),
        };
        return document.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    private JsonObject Policies()
    {
        var policies = new JsonObject();
        foreach (var gate in Gates)
        {
            policies[gate.Name] = gate.Policy;
        }

        return policies;
    }

    private JsonObject Reports()
    {
        var reports = new JsonObject();
        foreach (var gate in Gates)
        {
            reports[gate.Name] = gate.ToJson();
        }

        return reports;
    }

    private static JsonArray Strings(IReadOnlyList<string> values)
    {
        var array = new JsonArray();
        foreach (var value in values)
        {
            array.Add(value);
        }

        return array;
    }

    public static CheckReport Create(
        string tool,
        ReportStatus status,
        string? baseRef,
        IReadOnlyList<string> paths,
        IReadOnlyList<IGateReport> gates,
        IReadOnlyList<string> errors
    ) =>
        new(
            tool,
            ToolVersion.Current,
            status,
            Summarize(status, gates, errors),
            baseRef,
            paths,
            gates,
            errors
        );

    private static string Summarize(
        ReportStatus status,
        IReadOnlyList<IGateReport> gates,
        IReadOnlyList<string> errors
    )
    {
        if (status == ReportStatus.Error)
        {
            return errors.Count > 0 ? errors[0] : "Quality gate stopped with a tool error.";
        }

        if (status == ReportStatus.Fail)
        {
            var failed = string.Join(
                ", ",
                gates
                    .Where(static gate => gate.Status == GateStatus.Fail)
                    .Select(static gate => gate.Name)
            );
            return $"Quality gate failed: {failed}.";
        }

        var passed = gates
            .Where(static gate => gate.Status == GateStatus.Pass)
            .Select(static gate => gate.Name)
            .ToArray();
        var list = passed.Length == 0 ? "none" : string.Join(", ", passed);
        return $"All hard gates that ran passed ({list}).";
    }
}
