using System.Diagnostics;
using AgenticTestForge.Cli;
using AgenticTestForge.Config;

namespace AgenticTestForge.Tests;

[Trait("Category", "Unit")]
public class GitDiffTests
{
    [Fact]
    public void BaseLimitsAnalysisToTheDiffUnderConfiguredPaths()
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
        root.Write("tests/App.Tests/App.Tests.csproj", "<Project />");
        root.Write("src/App/A.cs", "class A {}");
        root.Write("src/Other/B.cs", "class B {}");
        Git(root.Path, "init", "-b", "main");
        Git(root.Path, "add", ".");
        Git(root.Path, "commit", "-m", "init");
        var baseRef = Git(root.Path, "rev-parse", "HEAD").Trim();
        root.Write("src/App/A.cs", "class A { void M() {} }");
        root.Write("src/Other/B.cs", "class B { void M() {} }");
        Git(root.Path, "add", ".");
        Git(root.Path, "commit", "-m", "change");

        var outcome = RunPlanResolver.Resolve(
            ForgeArgs.Parse(["check", "--base", baseRef]),
            root.Path
        );

        Assert.Null(outcome.Error);
        Assert.Equal(["src/App/A.cs"], outcome.Plan!.ScopedFiles);
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
