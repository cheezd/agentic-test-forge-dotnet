using AgenticTestForge.Mutation;
using AgenticTestForge.Reporting;

namespace AgenticTestForge.Tests;

[Trait("Category", "Unit")]
public class MutationScorerTests
{
    private const string Source = """
        namespace Sample;

        public static class Adder
        {
            public static int Add(int left, int right) => left + right;
        }
        """;

    [Fact]
    public void CompileErrorCountsAsKilled()
    {
        var score = MutationScorer.Score(
            [File([Mutant("CompileError", "Arithmetic mutation")])],
            80,
            @"C:\repo",
            ["src"],
            null
        );

        Assert.Equal(GateStatus.Pass, score.Status);
        var finding = Assert.Single(score.Findings);
        Assert.Equal(100d, finding.Score);
        Assert.Equal(1, finding.Killed);
        Assert.Equal(1, finding.Total);
        Assert.False(finding.AboveThreshold);
        Assert.Empty(score.Survivors);
        Assert.Empty(score.Inconclusive);
    }

    [Fact]
    public void SurvivorIsListedAndAPassingRateStillFailsWhenAMutantIsInconclusive()
    {
        var score = MutationScorer.Score(
            [
                File([
                    Mutant("Killed", "Arithmetic mutation"),
                    Mutant("Killed", "Arithmetic mutation"),
                    Mutant("Killed", "Arithmetic mutation"),
                    Mutant("Killed", "Arithmetic mutation"),
                    Mutant("Killed", "Arithmetic mutation"),
                    Mutant("Killed", "Arithmetic mutation"),
                    Mutant("Killed", "Arithmetic mutation"),
                    Mutant("Killed", "Arithmetic mutation"),
                    Mutant("Survived", "Equality mutation"),
                    Mutant("Timeout", "Statement mutation"),
                ]),
            ],
            80,
            @"C:\repo",
            ["src"],
            null
        );

        Assert.Equal(GateStatus.Fail, score.Status);
        var finding = Assert.Single(score.Findings);
        Assert.Equal(80d, finding.Score);
        Assert.False(finding.AboveThreshold);
        var survivor = Assert.Single(score.Survivors);
        Assert.Equal("src/Adder.cs", survivor.Filepath);
        Assert.Equal("Sample.Adder.Add", survivor.QualifiedName);
        Assert.Equal("Equality mutation", survivor.Mutator);
        Assert.Equal("Statement mutation", Assert.Single(score.Inconclusive).Mutator);
    }

    [Fact]
    public void ARateUnderTheFloorFailsAndAnEmptyDiffPasses()
    {
        var under = MutationScorer.Score(
            [
                File([
                    Mutant("Killed", "Arithmetic mutation"),
                    Mutant("Survived", "Equality mutation"),
                ]),
            ],
            80,
            @"C:\repo",
            ["src"],
            null
        );
        Assert.Equal(GateStatus.Fail, under.Status);
        Assert.True(Assert.Single(under.Findings).AboveThreshold);

        var onCeiling = MutationScorer.Score(
            [
                File([
                    Mutant("Killed", "Arithmetic mutation"),
                    Mutant("Killed", "Arithmetic mutation"),
                    Mutant("Killed", "Arithmetic mutation"),
                    Mutant("Killed", "Arithmetic mutation"),
                    Mutant("Survived", "Equality mutation"),
                ]),
            ],
            80,
            @"C:\repo",
            ["src"],
            ["src/Adder.cs"]
        );
        Assert.Equal(GateStatus.Pass, onCeiling.Status);
        Assert.Equal(80d, Assert.Single(onCeiling.Findings).Score);

        var empty = MutationScorer.Score(
            [File([Mutant("Survived", "Equality mutation")])],
            80,
            @"C:\repo",
            ["src"],
            []
        );
        Assert.Equal(GateStatus.Pass, empty.Status);
        Assert.Empty(empty.Findings);
    }

    [Fact]
    public void NoCoverageIsInconclusive()
    {
        var score = MutationScorer.Score(
            [File([Mutant("NoCoverage", "Arithmetic mutation")])],
            80,
            @"C:\repo",
            ["src"],
            null
        );

        Assert.Equal(GateStatus.Fail, score.Status);
        Assert.Empty(score.Survivors);
        Assert.Single(score.Inconclusive);
    }

    private static StrykerFile File(IReadOnlyList<StrykerMutant> mutants) =>
        new(@"C:\repo\src\Adder.cs", Source, mutants);

    private static StrykerMutant Mutant(string status, string mutator) => new(status, mutator, 5);
}
