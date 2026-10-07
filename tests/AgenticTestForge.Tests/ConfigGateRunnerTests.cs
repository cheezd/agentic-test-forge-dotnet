using System.Text.Json;
using AgenticTestForge.Cli;
using AgenticTestForge.Exit;
using AgenticTestForge.Reporting;

namespace AgenticTestForge.Tests;

[Trait("Category", "Unit")]
public class ConfigGateRunnerTests
{
    [Fact]
    public void CheckWithoutForgeJsonExitsTwo()
    {
        using var root = new TempDirectory();
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var code = ForgeApplication.Run(["check"], stdout, stderr, new ConfigGateRunner(root.Path));

        Assert.Equal(ExitCodes.Error, code);
        Assert.Contains("check: error", stdout.ToString(), StringComparison.Ordinal);
        Assert.Contains("forge.json was not found", stderr.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ResolvedProjectWithoutCoverletExitsTwo()
    {
        using var root = Ready();
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var reportPath = Path.Combine(root.Path, "report.json");

        var code = ForgeApplication.Run(
            ["check", "--json", reportPath],
            stdout,
            stderr,
            new ConfigGateRunner(root.Path)
        );

        Assert.Equal(ExitCodes.Error, code);
        Assert.Contains("check: error", stdout.ToString(), StringComparison.Ordinal);
        Assert.Contains("coverlet.collector", stderr.ToString(), StringComparison.Ordinal);

        using var document = JsonDocument.Parse(File.ReadAllText(reportPath));
        Assert.Equal("error", document.RootElement.GetProperty("status").GetString());
        Assert.Equal(
            "skipped",
            document
                .RootElement.GetProperty("reports")
                .GetProperty("gherkin")
                .GetProperty("status")
                .GetString()
        );
        Assert.Equal(
            "error",
            document
                .RootElement.GetProperty("reports")
                .GetProperty("crap")
                .GetProperty("status")
                .GetString()
        );
    }

    [Fact]
    public void DryIsSkippedAndDoesNotChangeTheExitCode()
    {
        using var root = new TempDirectory();
        root.Write("forge.json", """{"paths": ["src"]}""");
        var reportPath = Path.Combine(root.Path, "report.json");
        var stdout = new StringWriter();

        var code = ForgeApplication.Run(
            ["dry", "--json", reportPath],
            stdout,
            new StringWriter(),
            new ConfigGateRunner(root.Path)
        );

        Assert.Equal(ExitCodes.Pass, code);
        Assert.Contains("dry: pass", stdout.ToString(), StringComparison.Ordinal);
        using var document = JsonDocument.Parse(File.ReadAllText(reportPath));
        Assert.Equal("pass", document.RootElement.GetProperty("status").GetString());
        var dry = document.RootElement.GetProperty("reports").GetProperty("dry");
        Assert.Equal("skipped", dry.GetProperty("status").GetString());
        Assert.True(dry.GetProperty("advisory").GetBoolean());
    }

    [Fact]
    public void MutateGherkinIsSkippedUntilTheVerbExists()
    {
        using var root = new TempDirectory();
        root.Write("forge.json", """{"paths": ["src"]}""");
        var stdout = new StringWriter();

        var code = ForgeApplication.Run(
            ["mutate-gherkin"],
            stdout,
            new StringWriter(),
            new ConfigGateRunner(root.Path)
        );

        Assert.Equal(ExitCodes.Pass, code);
        Assert.Contains("mutate-gherkin: pass", stdout.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void MissingTestProjectOnCheckExitsTwo()
    {
        using var root = new TempDirectory();
        root.Write("forge.json", """{"paths": ["src/App"]}""");
        var stderr = new StringWriter();

        var code = ForgeApplication.Run(
            ["check"],
            new StringWriter(),
            stderr,
            new ConfigGateRunner(root.Path)
        );

        Assert.Equal(2, code);
        Assert.Contains("test_project is missing", stderr.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void MissingTestProjectWritesAnErrorReport()
    {
        using var root = new TempDirectory();
        root.Write("forge.json", """{"paths": ["src/App"]}""");
        var reportPath = Path.Combine(root.Path, "report.json");
        var stdout = new StringWriter();

        var code = ForgeApplication.Run(
            ["check", "--json", reportPath],
            stdout,
            new StringWriter(),
            new ConfigGateRunner(root.Path)
        );

        Assert.Equal(ExitCodes.Error, code);
        Assert.Contains("check: error", stdout.ToString(), StringComparison.Ordinal);
        using var document = JsonDocument.Parse(File.ReadAllText(reportPath));
        Assert.Equal("error", document.RootElement.GetProperty("status").GetString());
        Assert.Contains(
            "test_project is missing",
            document.RootElement.GetProperty("errors")[0].GetString(),
            StringComparison.Ordinal
        );
    }

    private static TempDirectory Ready()
    {
        var root = new TempDirectory();
        root.Write(
            "forge.json",
            """
            {
              "paths": ["src/App"],
              "test_project": "tests/App.Tests/App.Tests.csproj"
            }
            """
        );
        root.Write("tests/App.Tests/App.Tests.csproj", "<Project />");
        return root;
    }
}
