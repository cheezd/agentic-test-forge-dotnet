using AgenticTestForge.Mutation;

namespace AgenticTestForge.Tests;

[Trait("Category", "Unit")]
public class ProjectSelectorTests
{
    [Fact]
    public void SingleReferenceReturnsThatProject()
    {
        using var root = new TempDirectory();
        root.Write(
            "tests/App.Tests.csproj",
            """
            <Project>
              <ItemGroup>
                <ProjectReference Include="..\src\App.csproj" />
              </ItemGroup>
            </Project>
            """
        );

        var selected = ProjectSelector.Select(
            Path.Combine(root.Path, "tests", "App.Tests.csproj"),
            root.Path,
            ["src"]
        );

        Assert.Null(selected.Error);
        Assert.Equal("App.csproj", selected.Name);
    }

    [Fact]
    public void MultipleReferencesUseTheProjectThatOwnsThePath()
    {
        using var root = new TempDirectory();
        root.Write("src/App/App.csproj", "<Project />");
        root.Write("src/Other/Other.csproj", "<Project />");
        root.Write(
            "tests/App.Tests.csproj",
            """
            <Project>
              <ItemGroup>
                <ProjectReference Include="..\src\App\App.csproj" />
                <ProjectReference Include="..\src\Other\Other.csproj" />
              </ItemGroup>
            </Project>
            """
        );

        var selected = ProjectSelector.Select(
            Path.Combine(root.Path, "tests", "App.Tests.csproj"),
            root.Path,
            ["src/App"]
        );

        Assert.Null(selected.Error);
        Assert.Equal("App.csproj", selected.Name);
    }

    [Fact]
    public void MultipleReferencesWithoutOneOwningProjectAreAnError()
    {
        using var root = new TempDirectory();
        root.Write(
            "tests/App.Tests.csproj",
            """
            <Project>
              <ItemGroup>
                <ProjectReference Include="..\src\App\App.csproj" />
                <ProjectReference Include="..\src\Other\Other.csproj" />
              </ItemGroup>
            </Project>
            """
        );

        var selected = ProjectSelector.Select(
            Path.Combine(root.Path, "tests", "App.Tests.csproj"),
            root.Path,
            ["missing"]
        );

        Assert.Null(selected.Name);
        Assert.Equal("the test project references more than one project.", selected.Error);
    }

    [Fact]
    public void UnreadableProjectIsAnError()
    {
        using var root = new TempDirectory();
        root.Write("tests/App.Tests.csproj", "<Project>");

        var selected = ProjectSelector.Select(
            Path.Combine(root.Path, "tests", "App.Tests.csproj"),
            root.Path,
            ["src"]
        );

        Assert.Equal("the test project could not be read.", selected.Error);
    }

    [Fact]
    public void MissingProjectFileIsAnError()
    {
        using var root = new TempDirectory();

        var selected = ProjectSelector.Select(
            Path.Combine(root.Path, "tests", "Missing.csproj"),
            root.Path,
            ["src"]
        );

        Assert.Equal("the test project could not be read.", selected.Error);
    }
}
