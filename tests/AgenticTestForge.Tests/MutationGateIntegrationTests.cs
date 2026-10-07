using System.Text.Json;
using AgenticTestForge.Cli;
using AgenticTestForge.Exit;

namespace AgenticTestForge.Tests;

[Trait("Category", "Integration")]
public class MutationGateIntegrationTests
{
    [Fact]
    public void MutateExitsOneAndListsEachSurvivor()
    {
        using var root = Sample();
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var reportPath = Path.Combine(root.Path, "report.json");

        var code = ForgeApplication.Run(
            ["mutate", "--json", reportPath],
            stdout,
            stderr,
            new ConfigGateRunner(root.Path)
        );

        Assert.Equal(ExitCodes.Fail, code);
        Assert.Contains("mutate: fail", stdout.ToString(), StringComparison.Ordinal);

        using var document = JsonDocument.Parse(File.ReadAllText(reportPath));
        Assert.Equal("fail", document.RootElement.GetProperty("status").GetString());
        var mutation = document.RootElement.GetProperty("reports").GetProperty("mutation");
        var survivors = mutation.GetProperty("survivors");
        Assert.NotEmpty(survivors.EnumerateArray());
        var survivor = survivors[0];
        Assert.Contains(
            "Adder.cs",
            survivor.GetProperty("filepath").GetString(),
            StringComparison.Ordinal
        );
        Assert.Contains(
            "Positive",
            survivor.GetProperty("qualified_name").GetString(),
            StringComparison.Ordinal
        );
        Assert.False(string.IsNullOrWhiteSpace(survivor.GetProperty("mutator").GetString()));
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
              "test_project": "tests/Sample.Tests/Sample.Tests.csproj",
              "mutation_threshold": 80
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
            "src/Adder.cs",
            """
            namespace Sample;

            public static class Adder
            {
                public static int Positive(int value) => value > 0 ? value : 0;
            }
            """
        );
        root.Write(
            "tests/Sample.Tests/Sample.Tests.csproj",
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
                <IsPackable>false</IsPackable>
              </PropertyGroup>
              <ItemGroup>
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
            "tests/Sample.Tests/AdderTests.cs",
            """
            using Sample;

            public class AdderTests
            {
                [Fact]
                public void KeepsAPositiveValue()
                {
                    Assert.Equal(2, Adder.Positive(2));
                }
            }
            """
        );
        return root;
    }
}
