using AgenticTestForge.Cli;
using AgenticTestForge.Config;

namespace AgenticTestForge.Reporting;

internal sealed class GherkinQualityGate : IQualityGate
{
    public bool RunsFor(string verb) => verb is ForgeVerbs.Check or ForgeVerbs.MutateGherkin;

    public GateEvaluation Evaluate(RunPlan plan) =>
        new(GateReport.Gherkin(plan.GherkinThreshold, GateStatus.Skipped), null);
}
