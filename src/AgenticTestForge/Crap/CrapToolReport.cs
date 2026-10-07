using System.Text.Json;

namespace AgenticTestForge.Crap;

internal static class CrapToolReport
{
    public static IReadOnlyList<CrapToolMethod> Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("methods", out var methods))
        {
            return [];
        }

        var parsed = new List<CrapToolMethod>();
        foreach (var method in methods.EnumerateArray())
        {
            parsed.Add(
                new CrapToolMethod(
                    ReadString(method, "namespace"),
                    ReadString(method, "className"),
                    ReadString(method, "methodName"),
                    ReadString(method, "fullName"),
                    ReadString(method, "filePath"),
                    method.TryGetProperty("complexity", out var complexity)
                        ? complexity.GetDouble()
                        : 0d
                )
            );
        }

        return parsed;
    }

    private static string ReadString(JsonElement method, string name) =>
        method.TryGetProperty(name, out var value) ? value.GetString() ?? "" : "";
}
