using AgenticTestForge.Cli;
using AgenticTestForge.Config;
using AgenticTestForge.Gherkin;

namespace AgenticTestForge.Reporting;

internal sealed class GherkinQualityGate : IQualityGate
{
    public bool RunsFor(string verb) => verb is ForgeVerbs.Check or ForgeVerbs.MutateGherkin;

    public GateEvaluation Evaluate(RunPlan plan)
    {
        if (plan.SkipGherkin || string.IsNullOrWhiteSpace(plan.AcceptanceProject))
        {
            return new GateEvaluation(
                GateReport.Gherkin(plan.GherkinThreshold, GateStatus.Skipped),
                null
            );
        }

        var result = GherkinGate.Evaluate(plan);
        return new GateEvaluation(result.Gate, result.Error);
    }
}
