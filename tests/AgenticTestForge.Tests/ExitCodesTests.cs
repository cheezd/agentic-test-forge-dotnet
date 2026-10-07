using AgenticTestForge.Exit;
using AgenticTestForge.Reporting;

namespace AgenticTestForge.Tests;

[Trait("Category", "Unit")]
public class ExitCodesTests
{
    [Fact]
    public void MapsPassFailAndErrorToZeroOneAndTwo()
    {
        Assert.Equal(0, ExitCodes.Pass);
        Assert.Equal(1, ExitCodes.Fail);
        Assert.Equal(2, ExitCodes.Error);
        Assert.Equal(ExitCodes.Pass, ExitCodes.For(ReportStatus.Pass));
        Assert.Equal(ExitCodes.Fail, ExitCodes.For(ReportStatus.Fail));
        Assert.Equal(ExitCodes.Error, ExitCodes.For(ReportStatus.Error));
    }

    [Fact]
    public void WireNamesMatchTheReportContract()
    {
        Assert.Equal("pass", ReportStatus.Pass.ToWire());
        Assert.Equal("fail", ReportStatus.Fail.ToWire());
        Assert.Equal("error", ReportStatus.Error.ToWire());
    }
}
