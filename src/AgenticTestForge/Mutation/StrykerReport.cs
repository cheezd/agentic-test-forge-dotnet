using System.Text.Json;

namespace AgenticTestForge.Mutation;

internal static class StrykerReport
{
    public static IReadOnlyList<StrykerFile> Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("files", out var files))
        {
            return [];
        }

        var parsed = new List<StrykerFile>();
        foreach (var file in files.EnumerateObject())
        {
            var source = file.Value.TryGetProperty("source", out var sourceNode)
                ? sourceNode.GetString() ?? ""
                : "";
            var mutants = new List<StrykerMutant>();
            if (file.Value.TryGetProperty("mutants", out var list))
            {
                foreach (var mutant in list.EnumerateArray())
                {
                    mutants.Add(
                        new StrykerMutant(
                            mutant.TryGetProperty("status", out var status)
                                ? status.GetString() ?? ""
                                : "",
                            mutant.TryGetProperty("mutatorName", out var mutator)
                                ? mutator.GetString() ?? ""
                                : "",
                            Line(mutant)
                        )
                    );
                }
            }

            parsed.Add(new StrykerFile(file.Name, source, mutants));
        }

        return parsed;
    }

    private static int Line(JsonElement mutant)
    {
        if (
            mutant.TryGetProperty("location", out var location)
            && location.TryGetProperty("start", out var start)
            && start.TryGetProperty("line", out var line)
            && line.TryGetInt32(out var number)
        )
        {
            return number;
        }

        return 0;
    }
}
