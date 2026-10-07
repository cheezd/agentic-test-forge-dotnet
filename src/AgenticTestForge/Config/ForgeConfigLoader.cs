using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgenticTestForge.Config;

internal static class ForgeConfigLoader
{
    public const string FileName = "forge.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        AllowTrailingCommas = false,
        ReadCommentHandling = JsonCommentHandling.Disallow,
    };

    public static LoadResult Load(string startDirectory)
    {
        var root = FindRoot(startDirectory);
        if (root is null)
        {
            return LoadResult.Failed("forge.json was not found.");
        }

        var path = Path.Combine(root, FileName);
        Document? document;
        try
        {
            using var stream = File.OpenRead(path);
            document = JsonSerializer.Deserialize<Document>(stream, JsonOptions);
        }
        catch (JsonException exception)
        {
            return LoadResult.Failed($"forge.json is not valid JSON: {exception.Message}");
        }
        catch (IOException exception)
        {
            return LoadResult.Failed($"forge.json could not be read: {exception.Message}");
        }

        if (document is null)
        {
            return LoadResult.Failed("forge.json is not valid JSON.");
        }

        var thresholdError = document.ValidateThresholds();
        if (thresholdError is not null)
        {
            return LoadResult.Failed(thresholdError);
        }

        return LoadResult.Ok(root, document.ToConfig());
    }

    private static string? FindRoot(string startDirectory)
    {
        var current = new DirectoryInfo(Path.GetFullPath(startDirectory));
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, FileName)))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    private sealed class Document
    {
        [JsonPropertyName("paths")]
        public List<string>? Paths { get; set; }

        [JsonPropertyName("test_project")]
        public string? TestProject { get; set; }

        [JsonPropertyName("acceptance_project")]
        public string? AcceptanceProject { get; set; }

        [JsonPropertyName("crap_threshold")]
        public double? CrapThreshold { get; set; }

        [JsonPropertyName("mutation_threshold")]
        public double? MutationThreshold { get; set; }

        [JsonPropertyName("gherkin_threshold")]
        public double? GherkinThreshold { get; set; }

        [JsonPropertyName("dry_sources")]
        public List<string>? DrySources { get; set; }

        public string? ValidateThresholds()
        {
            if (!IsValid(CrapThreshold))
            {
                return "crap_threshold must be a number zero or greater.";
            }

            if (!IsValid(MutationThreshold))
            {
                return "mutation_threshold must be a number zero or greater.";
            }

            if (!IsValid(GherkinThreshold))
            {
                return "gherkin_threshold must be a number zero or greater.";
            }

            return null;
        }

        public ForgeConfig ToConfig() =>
            new(
                Paths is null ? [] : Paths,
                BlankToNull(TestProject),
                BlankToNull(AcceptanceProject),
                CrapThreshold ?? ForgeConfig.DefaultCrapThreshold,
                MutationThreshold ?? ForgeConfig.DefaultMutationThreshold,
                GherkinThreshold ?? ForgeConfig.DefaultGherkinThreshold,
                Sources(DrySources)
            );

        private static IReadOnlyList<string> Sources(List<string>? values)
        {
            if (values is null)
            {
                return ForgeConfig.DefaultDrySources;
            }

            return values
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Select(static value => value.Trim())
                .ToArray();
        }

        private static bool IsValid(double? value) =>
            value is null || (double.IsFinite(value.Value) && value.Value >= 0);

        private static string? BlankToNull(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
