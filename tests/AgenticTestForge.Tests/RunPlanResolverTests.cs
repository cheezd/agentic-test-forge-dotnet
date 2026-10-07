using AgenticTestForge.Cli;
using AgenticTestForge.Config;

namespace AgenticTestForge.Tests;

[Trait("Category", "Unit")]
public class RunPlanResolverTests
{
    [Fact]
    public void MissingTestProjectExitsTheVerbsThatNeedIt()
    {
        using var root = WriteConfig("""{"paths": ["src/App"]}""");

        foreach (var verb in new[] { "check", "crap", "mutate" })
        {
            var outcome = RunPlanResolver.Resolve(ForgeArgs.Parse([verb]), root.Path, NoDiff);
            Assert.Contains("test_project is missing", outcome.Error, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void DryAndGherkinDoNotRequireATestProject()
    {
        using var root = WriteConfig("""{"paths": ["src/App"]}""");

        foreach (var verb in new[] { "dry", "mutate-gherkin" })
        {
            var outcome = RunPlanResolver.Resolve(ForgeArgs.Parse([verb]), root.Path, NoDiff);
            Assert.Null(outcome.Error);
            Assert.True(outcome.Plan!.SkipGherkin);
        }
    }

    [Fact]
    public void MissingTestProjectFileIsAToolError()
    {
        using var root = WriteConfig("""{"test_project": "tests/Missing.csproj"}""");

        var outcome = RunPlanResolver.Resolve(ForgeArgs.Parse(["check"]), root.Path, NoDiff);

        Assert.Contains("was not found", outcome.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void RepeatedPathReplacesConfiguredRoots()
    {
        using var root = WriteReadyRepo();
        var parsed = ForgeArgs.Parse([
            "crap",
            "--base",
            "main",
            "--path",
            "src/Other",
            "--path",
            "src/Extra",
        ]);

        var plan = RunPlanResolver.Resolve(parsed, root.Path, DiffOfAppAndOther).Plan!;

        Assert.Equal(["src/Other", "src/Extra"], plan.Paths);
        Assert.Equal(["src/Other/B.cs"], plan.ScopedFiles);
    }

    [Fact]
    public void OnePathReplacesConfiguredRoots()
    {
        using var root = WriteReadyRepo();
        var parsed = ForgeArgs.Parse(["check", "--base", "main", "--path", "src/Other"]);

        var plan = RunPlanResolver.Resolve(parsed, root.Path, DiffOfAppAndOther).Plan!;

        Assert.Equal(["src/Other"], plan.Paths);
        Assert.Equal(["src/Other/B.cs"], plan.ScopedFiles);
    }

    [Fact]
    public void WithoutPathTheConfiguredRootsLimitTheDiff()
    {
        using var root = WriteReadyRepo();

        var plan = RunPlanResolver
            .Resolve(ForgeArgs.Parse(["check", "--base", "main"]), root.Path, DiffOfAppAndOther)
            .Plan!;

        Assert.Equal(["src/App"], plan.Paths);
        Assert.Equal("main", plan.BaseRef);
        Assert.Equal(["src/App/A.cs"], plan.ScopedFiles);
    }

    [Fact]
    public void WithoutBaseTheRunKeepsTheConfiguredPaths()
    {
        using var root = WriteReadyRepo();

        var plan = RunPlanResolver.Resolve(ForgeArgs.Parse(["check"]), root.Path, NoDiff).Plan!;

        Assert.Null(plan.BaseRef);
        Assert.Null(plan.ScopedFiles);
        Assert.Equal(["src/App"], plan.Paths);
        Assert.Equal(30, plan.CrapThreshold);
        Assert.Equal(80, plan.MutationThreshold);
        Assert.Equal(80, plan.GherkinThreshold);
        Assert.True(plan.SkipGherkin);
    }

    [Theory]
    [InlineData("crap", 12)]
    [InlineData("mutate", 55)]
    [InlineData("mutate-gherkin", 66)]
    public void ThresholdOverridesOnlyThatVerb(string verb, double threshold)
    {
        using var root = WriteReadyRepo(
            """
            {
              "paths": ["src/App"],
              "test_project": "tests/App.Tests/App.Tests.csproj",
              "acceptance_project": "tests/App.Acceptance/App.Acceptance.csproj",
              "crap_threshold": 30,
              "mutation_threshold": 80,
              "gherkin_threshold": 80
            }
            """
        );
        root.Write("tests/App.Acceptance/App.Acceptance.csproj", "<Project />");

        var plan = RunPlanResolver
            .Resolve(
                ForgeArgs.Parse([
                    verb,
                    "--threshold",
                    threshold.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ]),
                root.Path,
                NoDiff
            )
            .Plan!;

        Assert.Equal(verb == "crap" ? threshold : 30, plan.CrapThreshold);
        Assert.Equal(verb == "mutate" ? threshold : 80, plan.MutationThreshold);
        Assert.Equal(verb == "mutate-gherkin" ? threshold : 80, plan.GherkinThreshold);
    }

    [Fact]
    public void CheckUsesConfiguredThresholds()
    {
        using var root = WriteReadyRepo(
            """
            {
              "paths": ["src/App"],
              "test_project": "tests/App.Tests/App.Tests.csproj",
              "crap_threshold": 22,
              "mutation_threshold": 77,
              "gherkin_threshold": 66
            }
            """
        );

        var plan = RunPlanResolver.Resolve(ForgeArgs.Parse(["check"]), root.Path, NoDiff).Plan!;

        Assert.Equal(22, plan.CrapThreshold);
        Assert.Equal(77, plan.MutationThreshold);
        Assert.Equal(66, plan.GherkinThreshold);
    }

    [Fact]
    public void JsonPathIsKept()
    {
        using var root = WriteReadyRepo();

        var plan = RunPlanResolver
            .Resolve(ForgeArgs.Parse(["check", "--json", "report.json"]), root.Path, NoDiff)
            .Plan!;

        Assert.Equal("report.json", plan.JsonPath);
    }

    [Fact]
    public void ConfiguredAcceptanceProjectIsResolved()
    {
        using var root = WriteReadyRepo(
            """
            {
              "paths": ["src/App"],
              "test_project": "tests/App.Tests/App.Tests.csproj",
              "acceptance_project": "tests/App.Acceptance/App.Acceptance.csproj"
            }
            """
        );
        root.Write("tests/App.Acceptance/App.Acceptance.csproj", "<Project />");

        var plan = RunPlanResolver.Resolve(ForgeArgs.Parse(["check"]), root.Path, NoDiff).Plan!;

        Assert.False(plan.SkipGherkin);
        Assert.EndsWith("App.Acceptance.csproj", plan.AcceptanceProject, StringComparison.Ordinal);
    }

    [Fact]
    public void MissingAcceptanceFileIsAToolError()
    {
        using var root = WriteReadyRepo(
            """
            {
              "test_project": "tests/App.Tests/App.Tests.csproj",
              "acceptance_project": "tests/Missing.csproj"
            }
            """
        );

        var outcome = RunPlanResolver.Resolve(
            ForgeArgs.Parse(["mutate-gherkin"]),
            root.Path,
            NoDiff
        );

        Assert.Contains("acceptance_project", outcome.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void GitFailureIsAToolError()
    {
        using var root = WriteReadyRepo();

        var outcome = RunPlanResolver.Resolve(
            ForgeArgs.Parse(["check", "--base", "missing"]),
            root.Path,
            (_, _) => (null, "git diff failed for base ref 'missing'.")
        );

        Assert.Contains("git diff failed", outcome.Error, StringComparison.Ordinal);
    }

    private static (IReadOnlyList<string>? Files, string? Error) NoDiff(
        string repoRoot,
        string baseRef
    )
    {
        _ = repoRoot;
        _ = baseRef;
        throw new InvalidOperationException("This run has no --base.");
    }

    private static (IReadOnlyList<string>? Files, string? Error) DiffOfAppAndOther(
        string repoRoot,
        string baseRef
    )
    {
        _ = repoRoot;
        _ = baseRef;
        return (["src/App/A.cs", "src/Other/B.cs"], null);
    }

    private static TempDirectory WriteReadyRepo() =>
        WriteReadyRepo(
            """
            {
              "paths": ["src/App"],
              "test_project": "tests/App.Tests/App.Tests.csproj"
            }
            """
        );

    private static TempDirectory WriteReadyRepo(string json)
    {
        var root = WriteConfig(json);
        root.Write("tests/App.Tests/App.Tests.csproj", "<Project />");
        return root;
    }

    private static TempDirectory WriteConfig(string json)
    {
        var root = new TempDirectory();
        root.Write("forge.json", json);
        return root;
    }
}
