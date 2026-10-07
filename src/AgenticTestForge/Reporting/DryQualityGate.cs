using AgenticTestForge.Cli;
using AgenticTestForge.Config;
using AgenticTestForge.Dry;

namespace AgenticTestForge.Reporting;

internal sealed class DryQualityGate : IQualityGate
{
    public bool RunsFor(string verb) => verb is ForgeVerbs.Check or ForgeVerbs.Dry;

    public GateEvaluation Evaluate(RunPlan plan) => DryGate.Evaluate(plan);
}
