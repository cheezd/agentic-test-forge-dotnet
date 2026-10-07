using AgenticTestForge.Crap;
using AgenticTestForge.Reporting;

namespace AgenticTestForge.Tests;

[Trait("Category", "Unit")]
public class CrapScoreTests
{
    [Fact]
    public void FullCoverageLeavesTheScoreEqualToComplexity() =>
        Assert.Equal(3d, CrapScore.Compute(3d, 1d));

    [Fact]
    public void ZeroCoverageAddsTheSquaredComplexity()
    {
        Assert.Equal(20d, CrapScore.Compute(4d, 0d));
        Assert.Equal(42d, CrapScore.Compute(6d, 0d));
    }

    [Fact]
    public void CeilingIsExclusive()
    {
        Assert.False(ThresholdRules.CrapIsAboveCeiling(30d, 30d));
        Assert.True(ThresholdRules.CrapIsAboveCeiling(30.1d, 30d));
        Assert.True(ThresholdRules.CrapIsAboveCeiling(null, 30d));
    }
}
