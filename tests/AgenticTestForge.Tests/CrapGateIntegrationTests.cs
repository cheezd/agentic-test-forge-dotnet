using System.Text.Json;
using AgenticTestForge.Cli;
using AgenticTestForge.Config;
using AgenticTestForge.Exit;
using AgenticTestForge.Reporting;

namespace AgenticTestForge.Tests;

[Trait("Category", "Integration")]
public class CrapGateIntegrationTests
{
    [Fact]
    public void CrapExitsOneWhenAMemberIsOverTheCeilingAndFoldsGeneratedMethods()
    {
        using var root = Sample();
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var reportPath = Path.Combine(root.Path, "report.json");

        var code = ForgeApplication.Run(
            ["crap", "--json", reportPath],
            stdout,
            stderr,
            new ConfigGateRunner(root.Path)
        );

        Assert.Equal(ExitCodes.Fail, code);
        Assert.Contains("crap: fail", stdout.ToString(), StringComparison.Ordinal);
        Assert.Empty(stderr.ToString());

        using var document = JsonDocument.Parse(File.ReadAllText(reportPath));
        Assert.Equal("fail", document.RootElement.GetProperty("status").GetString());
        var findings = document
            .RootElement.GetProperty("reports")
            .GetProperty("crap")
            .GetProperty("findings");

        var risky = Find(findings, "Risky");
        Assert.True(risky.GetProperty("above_threshold").GetBoolean());
        Assert.Equal(0d, risky.GetProperty("coverage").GetDouble());
        Assert.True(risky.GetProperty("crap_score").GetDouble() > 30d);
        Assert.True(risky.GetProperty("complexity").GetDouble() >= 6d);

        var fetch = Find(findings, "Fetch");
        Assert.False(fetch.GetProperty("above_threshold").GetBoolean());
        Assert.True(fetch.GetProperty("coverage").GetDouble() > 0d);

        var range = Find(findings, "Range");
        Assert.False(range.GetProperty("above_threshold").GetBoolean());
        Assert.True(range.GetProperty("coverage").GetDouble() > 0d);

        foreach (var finding in findings.EnumerateArray())
        {
            var name = finding.GetProperty("qualified_name").GetString() ?? "";
            Assert.DoesNotContain("MoveNext", name, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void EmptyDiffPassesWithoutCoverage()
    {
        var plan = new RunPlan(
            ForgeVerbs.Crap,
            Directory.GetCurrentDirectory(),
            ["src"],
            "main",
            [],
            null,
            30d,
            80d,
            80d,
            null,
            null,
            true,
            ForgeConfig.DefaultDrySources
        );

        var evaluation = AgenticTestForge.Crap.CrapGate.Evaluate(plan);

        Assert.Null(evaluation.Error);
        Assert.Equal(GateStatus.Pass, evaluation.Gate.Status);
        Assert.Empty(Assert.IsType<CrapGateReport>(evaluation.Gate).Findings);
    }

    private static JsonElement Find(JsonElement findings, string name)
    {
        foreach (var finding in findings.EnumerateArray())
        {
            var qualified = finding.GetProperty("qualified_name").GetString() ?? "";
            if (qualified.Contains(name, StringComparison.Ordinal))
            {
                return finding;
            }
        }

        throw new Xunit.Sdk.XunitException($"Finding '{name}' was not in the CRAP report.");
    }

    private static TempDirectory Sample()
    {
        var root = new TempDirectory();
        root.Write(
            "nuget.config",
            """
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <clear />
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
              </packageSources>
            </configuration>
            """
        );
        root.Write(
            "forge.json",
            """
            {
              "paths": ["src"],
              "test_project": "tests/Widget.Tests/Widget.Tests.csproj",
              "crap_threshold": 30
            }
            """
        );
        root.Write(
            "src/Sample.csproj",
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
              </PropertyGroup>
            </Project>
            """
        );
        root.Write(
            "src/Widget.cs",
            """
            namespace Sample;

            public sealed class Widget
            {
                public int Score(int value) => value > 0 ? 1 : 0;

                public async Task<int> Fetch(int n)
                {
                    await Task.Yield();
                    if (n > 0)
                    {
                        return n;
                    }

                    return 0;
                }

                public IEnumerable<int> Range(int n)
                {
                    for (var i = 0; i < n; i++)
                    {
                        yield return i;
                    }
                }

                public int Risky(int n)
                {
                    if (n > 100)
                    {
                        return 1;
                    }

                    if (n > 80)
                    {
                        return 2;
                    }

                    if (n > 60)
                    {
                        return 3;
                    }

                    if (n > 40)
                    {
                        return 4;
                    }

                    if (n > 20)
                    {
                        return 5;
                    }

                    if (n > 0)
                    {
                        return 6;
                    }

                    return 0;
                }
            }
            """
        );
        root.Write(
            "tests/Widget.Tests/Widget.Tests.csproj",
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
                <IsPackable>false</IsPackable>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="coverlet.collector" Version="10.1.0" />
                <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.10.1" />
                <PackageReference Include="xunit" Version="2.9.3" />
                <PackageReference Include="xunit.runner.visualstudio" Version="4.0.0" />
              </ItemGroup>
              <ItemGroup>
                <Using Include="Xunit" />
                <ProjectReference Include="..\..\src\Sample.csproj" />
              </ItemGroup>
            </Project>
            """
        );
        root.Write(
            "tests/Widget.Tests/WidgetTests.cs",
            """
            using Sample;

            public class WidgetTests
            {
                [Fact]
                public void ScoreIsPositive()
                {
                    Assert.Equal(1, new Widget().Score(2));
                }

                [Fact]
                public async Task FetchIsPositive()
                {
                    Assert.Equal(2, await new Widget().Fetch(2));
                }

                [Fact]
                public void RangeYields()
                {
                    Assert.Equal(new[] { 0, 1 }, new Widget().Range(2));
                }
            }
            """
        );
        return root;
    }
}
