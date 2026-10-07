using System.Diagnostics;
using System.Text.Json;
using AgenticTestForge.Cli;
using AgenticTestForge.Exit;

namespace AgenticTestForge.Tests;

[Trait("Category", "Integration")]
public class CheckCommandIntegrationTests
{
    [Fact]
    public void CheckOnADiffExitsOneAndListsMembersAndSurvivors()
    {
        using var root = new TempDirectory();
        root.Write("README.md", "base");
        Git(root.Path, "init", "-b", "main");
        Git(root.Path, "add", ".");
        Git(root.Path, "commit", "-m", "base");
        var baseRef = Git(root.Path, "rev-parse", "HEAD").Trim();
        WriteProduct(root);
        Git(root.Path, "add", ".");
        Git(root.Path, "commit", "-m", "change");

        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var reportPath = Path.Combine(root.Path, "report.json");
        var code = ForgeApplication.Run(
            ["check", "--base", baseRef, "--json", reportPath],
            stdout,
            stderr,
            new ConfigGateRunner(root.Path)
        );

        Assert.Equal(ExitCodes.Fail, code);
        Assert.Contains("check: fail", stdout.ToString(), StringComparison.Ordinal);
        Assert.Empty(stderr.ToString());

        using var document = JsonDocument.Parse(File.ReadAllText(reportPath));
        Assert.Equal("fail", document.RootElement.GetProperty("status").GetString());
        Assert.Equal(
            baseRef,
            document.RootElement.GetProperty("scope").GetProperty("base").GetString()
        );

        var reports = document.RootElement.GetProperty("reports");
        var crap = reports.GetProperty("crap").GetProperty("findings");
        var risky = Find(crap, "Risky");
        Assert.True(risky.GetProperty("above_threshold").GetBoolean());
        Assert.True(risky.GetProperty("crap_score").GetDouble() > 30d);

        var survivors = reports.GetProperty("mutation").GetProperty("survivors");
        Assert.Contains(
            survivors.EnumerateArray(),
            survivor =>
                (survivor.GetProperty("qualified_name").GetString() ?? "").Contains(
                    "Positive",
                    StringComparison.Ordinal
                )
                && !string.IsNullOrWhiteSpace(survivor.GetProperty("mutator").GetString())
                && (survivor.GetProperty("filepath").GetString() ?? "").Contains(
                    "Widget.cs",
                    StringComparison.Ordinal
                )
        );

        Assert.Equal("skipped", reports.GetProperty("gherkin").GetProperty("status").GetString());
        var dry = reports.GetProperty("dry");
        Assert.Equal("skipped", dry.GetProperty("status").GetString());
        Assert.True(dry.GetProperty("advisory").GetBoolean());
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

        throw new Xunit.Sdk.XunitException($"Finding '{name}' was not in the report.");
    }

    private static void WriteProduct(TempDirectory root)
    {
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
              "test_project": "tests/Widget.Tests/Widget.Tests.csproj"
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

                public int Positive(int value) => value > 0 ? value : 0;

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
                public void KeepsAPositiveValue()
                {
                    Assert.Equal(2, new Widget().Positive(2));
                }
            }
            """
        );
    }

    private static string Git(string root, params string[] args)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        if (args is ["commit", ..])
        {
            process.StartInfo.ArgumentList.Add("-c");
            process.StartInfo.ArgumentList.Add("user.email=forge-tests@example.com");
            process.StartInfo.ArgumentList.Add("-c");
            process.StartInfo.ArgumentList.Add("user.name=forge-tests");
        }

        foreach (var arg in args)
        {
            process.StartInfo.ArgumentList.Add(arg);
        }

        process.Start();
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(stderr);
        }

        return stdout;
    }
}
