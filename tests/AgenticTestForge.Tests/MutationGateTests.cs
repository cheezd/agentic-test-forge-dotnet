using AgenticTestForge.Cli;
using AgenticTestForge.Config;
using AgenticTestForge.Mutation;
using AgenticTestForge.Reporting;

namespace AgenticTestForge.Tests;

[Trait("Category", "Unit")]
public class MutationGateTests
{
    [Fact]
    public void EmptyDiffPassesWithoutSelectingAProject()
    {
        var called = false;

        var evaluation = MutationGate.Evaluate(
            Plan(Directory.GetCurrentDirectory(), "tests/App.Tests/App.Tests.csproj", ["src"], []),
            (_, _, _) =>
            {
                called = true;
                return (null, "stopped");
            }
        );

        Assert.False(called);
        Assert.Null(evaluation.Error);
        Assert.Equal(GateStatus.Pass, evaluation.Gate.Status);
    }

    [Fact]
    public void MissingDiffSelectsFromTheConfiguredPaths()
    {
        IReadOnlyList<string>? passed = null;

        var evaluation = MutationGate.Evaluate(
            Plan(
                Directory.GetCurrentDirectory(),
                "tests/App.Tests/App.Tests.csproj",
                ["src/First", "src/Fourth"],
                null
            ),
            (_, _, paths) =>
            {
                passed = paths;
                return (null, "stopped");
            }
        );

        Assert.Equal(["src/First", "src/Fourth"], passed);
        Assert.Equal("stopped", evaluation.Error);
    }

    [Fact]
    public void NonEmptyDiffSelectsTheProjectFromTheChangedFiles()
    {
        using var root = WriteShape();
        IReadOnlyList<string>? passed = null;
        (string? Name, string? Error) selected = default;

        var evaluation = MutationGate.Evaluate(
            Plan(
                root.Path,
                Path.Combine(root.Path, "tests", "App.Tests", "App.Tests.csproj"),
                ["src/First", "src/Second", "src/Third", "src/Fourth"],
                ["src/First/Reader.cs", "tests/App.Tests/ReaderTests.cs"]
            ),
            (testProject, repoRoot, paths) =>
            {
                passed = paths;
                selected = ProjectSelector.Select(testProject, repoRoot, paths);
                return (null, selected.Error ?? selected.Name);
            }
        );

        Assert.Equal(["src/First/Reader.cs", "tests/App.Tests/ReaderTests.cs"], passed);
        Assert.Null(selected.Error);
        Assert.Equal("First.csproj", selected.Name);
        Assert.Equal("First.csproj", evaluation.Error);
    }

    [Fact]
    public void DiffAcrossTwoReferencedProjectsIsAToolError()
    {
        using var root = WriteShape();

        var evaluation = MutationGate.Evaluate(
            Plan(
                root.Path,
                Path.Combine(root.Path, "tests", "App.Tests", "App.Tests.csproj"),
                ["src/First", "src/Second", "src/Third", "src/Fourth"],
                ["src/First/Reader.cs", "src/Fourth/Worker.cs"]
            )
        );

        Assert.Equal("the test project references more than one project.", evaluation.Error);
    }

    private static TempDirectory WriteShape()
    {
        var root = new TempDirectory();
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
        return root;
    }

    private static RunPlan Plan(
        string root,
        string testProject,
        IReadOnlyList<string> paths,
        IReadOnlyList<string>? scopedFiles
    ) =>
        new(
            ForgeVerbs.Check,
            root,
            paths,
            "main",
            scopedFiles,
            null,
            30,
            80,
            80,
            testProject,
            null,
            true,
            ForgeConfig.DefaultDrySources
        );
}
