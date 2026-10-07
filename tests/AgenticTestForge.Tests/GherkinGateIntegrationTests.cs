using System.Text.Json;
using AgenticTestForge.Cli;
using AgenticTestForge.Exit;

namespace AgenticTestForge.Tests;

[Trait("Category", "Integration")]
public class GherkinGateIntegrationTests
{
    [Fact]
    public void MutateGherkinExitsOneWhenAnExampleStillPasses()
    {
        using var root = Sample();
        var reportPath = Path.Combine(root.Path, "report.json");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var code = ForgeApplication.Run(
            ["mutate-gherkin", "--json", reportPath],
            stdout,
            stderr,
            new ConfigGateRunner(root.Path)
        );

        Assert.Equal(ExitCodes.Fail, code);
        Assert.Contains("mutate-gherkin: fail", stdout.ToString(), StringComparison.Ordinal);

        using var document = JsonDocument.Parse(File.ReadAllText(reportPath));
        Assert.Equal("fail", document.RootElement.GetProperty("status").GetString());
        var gherkin = document.RootElement.GetProperty("reports").GetProperty("gherkin");
        var finding = Assert.Single(gherkin.GetProperty("findings").EnumerateArray());
        Assert.Equal("Positive stays positive", finding.GetProperty("scenario").GetString());
        Assert.True(finding.GetProperty("above_threshold").GetBoolean());
        Assert.True(finding.GetProperty("killed").GetInt32() >= 1);
        Assert.Contains(
            gherkin.GetProperty("survivors").EnumerateArray(),
            survivor => survivor.GetProperty("mutator").GetString() == "2 -> 3"
        );
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
              "paths": ["tests/Acceptance"],
              "acceptance_project": "tests/Acceptance/Acceptance.csproj",
              "gherkin_threshold": 80
            }
            """
        );
        root.Write(
            "tests/Acceptance/Acceptance.csproj",
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
                <IsTestProject>true</IsTestProject>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.10.1" />
                <PackageReference Include="Reqnroll.xUnit" Version="3.3.4" />
                <PackageReference Include="xunit" Version="2.9.3" />
                <PackageReference Include="xunit.runner.visualstudio" Version="4.0.0">
                  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
                  <PrivateAssets>all</PrivateAssets>
                </PackageReference>
              </ItemGroup>
            </Project>
            """
        );
        root.Write(
            "tests/Acceptance/Score.feature",
            """
            Feature: Score

              Scenario Outline: Positive stays positive
                Given a value of <value>
                Then the value is positive

                Examples:
                  | value |
                  | 2     |
            """
        );
        root.Write(
            "tests/Acceptance/ScoreSteps.cs",
            """
            using Reqnroll;

            namespace Acceptance;

            [Binding]
            public class ScoreSteps
            {
                private int _value;

                [Given("a value of {int}")]
                public void GivenAValue(int value) => _value = value;

                [Then("the value is positive")]
                public void ThenTheValueIsPositive() => Xunit.Assert.True(_value > 0);
            }
            """
        );
        return root;
    }
}
