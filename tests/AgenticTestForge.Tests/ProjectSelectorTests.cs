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

    [Fact]
    public void MultipleReferencesFollowTheChangedFilesNotEveryConfiguredPath()
    {
        using var root = new TempDirectory();
        root.Write("src/First/First.csproj", "<Project />");
        root.Write("src/Second/Second.csproj", "<Project />");
        root.Write("src/Third/Third.csproj", "<Project />");
        root.Write("src/Fourth/Fourth.csproj", "<Project />");
        root.Write("src/First/Reader.cs", "class Reader { }");
        root.Write("src/Fourth/Worker.cs", "class Worker { }");
        root.Write("tests/App.Tests/ReaderTests.cs", "class ReaderTests { }");
        root.Write(
            "tests/App.Tests/App.Tests.csproj",
            """
            <Project>
              <ItemGroup>
                <ProjectReference Include="..\..\src\First\First.csproj" />
                <ProjectReference Include="..\..\src\Fourth\Fourth.csproj" />
              </ItemGroup>
            </Project>
            """
        );

        var testProject = Path.Combine(root.Path, "tests", "App.Tests", "App.Tests.csproj");
        IReadOnlyList<string> configured = ["src/First", "src/Second", "src/Third", "src/Fourth"];

        var allPaths = ProjectSelector.Select(testProject, root.Path, configured);
        Assert.Null(allPaths.Name);
        Assert.Equal("the test project references more than one project.", allPaths.Error);

        var oneFile = ProjectSelector.Select(testProject, root.Path, ["src/First/Reader.cs"]);
        Assert.Null(oneFile.Error);
        Assert.Equal("First.csproj", oneFile.Name);

        var fileAndTest = ProjectSelector.Select(
            testProject,
            root.Path,
            ["src/First/Reader.cs", "tests/App.Tests/ReaderTests.cs"]
        );
        Assert.Null(fileAndTest.Error);
        Assert.Equal("First.csproj", fileAndTest.Name);

        var twoProjects = ProjectSelector.Select(
            testProject,
            root.Path,
            ["src/First/Reader.cs", "src/Fourth/Worker.cs"]
        );
        Assert.Null(twoProjects.Name);
        Assert.Equal("the test project references more than one project.", twoProjects.Error);
    }
}
