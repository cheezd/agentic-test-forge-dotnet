using AgenticTestForge.Cli;
using AgenticTestForge.Config;

namespace AgenticTestForge.Reporting;

internal sealed class DryQualityGate : IQualityGate
{
    public bool RunsFor(string verb) => verb is ForgeVerbs.Check or ForgeVerbs.Dry;

    public GateEvaluation Evaluate(RunPlan plan) =>
        new(GateReport.Dry(GateStatus.Skipped, []), null);
}
