using AgenticTestForge.Config;

namespace AgenticTestForge.Tests;

[Trait("Category", "Unit")]
public class ForgeConfigLoaderTests
{
    [Fact]
    public void ReadsTheSampleFile()
    {
        using var root = new TempDirectory();
        root.Write(
            "forge.json",
            """
            {
              "paths": ["src/App"],
              "test_project": "tests/App.Tests/App.Tests.csproj",
              "acceptance_project": "tests/App.Acceptance/App.Acceptance.csproj",
              "crap_threshold": 25,
              "mutation_threshold": 90,
              "gherkin_threshold": 70
            }
            """
        );

        var loaded = ForgeConfigLoader.Load(root.Path);

        Assert.Null(loaded.Error);
        Assert.Equal(root.Path, loaded.RepoRoot);
        var config = loaded.Config!;
        Assert.Equal(["src/App"], config.Paths);
        Assert.Equal("tests/App.Tests/App.Tests.csproj", config.TestProject);
        Assert.Equal("tests/App.Acceptance/App.Acceptance.csproj", config.AcceptanceProject);
        Assert.Equal(25, config.CrapThreshold);
        Assert.Equal(90, config.MutationThreshold);
        Assert.Equal(70, config.GherkinThreshold);
    }

    [Fact]
    public void OmittedThresholdsDefaultToThirtyAndEighty()
    {
        using var root = new TempDirectory();
        root.Write(
            "forge.json",
            """
            {
              "paths": ["src/App"],
              "test_project": "tests/App.Tests/App.Tests.csproj"
            }
            """
        );

        var config = ForgeConfigLoader.Load(root.Path).Config!;

        Assert.Equal(ForgeConfig.DefaultCrapThreshold, config.CrapThreshold);
        Assert.Equal(ForgeConfig.DefaultMutationThreshold, config.MutationThreshold);
        Assert.Equal(ForgeConfig.DefaultGherkinThreshold, config.GherkinThreshold);
        Assert.Equal(30, config.CrapThreshold);
        Assert.Equal(80, config.MutationThreshold);
        Assert.Equal(80, config.GherkinThreshold);
        Assert.Null(config.AcceptanceProject);
        Assert.Equal(ForgeConfig.DefaultDrySources, config.DrySources);
    }

    [Fact]
    public void DrySourcesKeepAnEmptyListAndANamedReplacement()
    {
        using var root = new TempDirectory();
        root.Write(
            "forge.json",
            """
            { "dry_sources": ["sonar", "jaccard", " "] }
            """
        );

        var listed = ForgeConfigLoader.Load(root.Path).Config!;

        Assert.Equal(["sonar", "jaccard"], listed.DrySources);

        root.Write("forge.json", """{ "dry_sources": [] }""");

        var empty = ForgeConfigLoader.Load(root.Path).Config!;

        Assert.Empty(empty.DrySources);
    }

    [Fact]
    public void NullAcceptanceProjectSkipsGherkinConfiguration()
    {
        using var root = new TempDirectory();
        root.Write(
            "forge.json",
            """
            {
              "test_project": "tests/App.Tests/App.Tests.csproj",
              "acceptance_project": null
            }
            """
        );

        var config = ForgeConfigLoader.Load(root.Path).Config!;

        Assert.Null(config.AcceptanceProject);
    }

    [Fact]
    public void MissingFileIsAToolError()
    {
        using var root = new TempDirectory();

        var loaded = ForgeConfigLoader.Load(root.Path);

        Assert.Contains("forge.json was not found", loaded.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void FindsTheFileInAParentDirectory()
    {
        using var root = new TempDirectory();
        root.Write("forge.json", """{"test_project": "App.Tests.csproj"}""");
        var nested = Path.Combine(root.Path, "src", "App");
        Directory.CreateDirectory(nested);

        var loaded = ForgeConfigLoader.Load(nested);

        Assert.Null(loaded.Error);
        Assert.Equal(root.Path, loaded.RepoRoot);
    }

    [Fact]
    public void InvalidJsonIsAToolError()
    {
        using var root = new TempDirectory();
        root.Write("forge.json", "{");

        var loaded = ForgeConfigLoader.Load(root.Path);

        Assert.Contains("not valid JSON", loaded.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void NegativeThresholdIsAToolError()
    {
        using var root = new TempDirectory();
        root.Write("forge.json", """{"crap_threshold": -1}""");

        var loaded = ForgeConfigLoader.Load(root.Path);

        Assert.Contains("crap_threshold", loaded.Error, StringComparison.Ordinal);
    }
}
