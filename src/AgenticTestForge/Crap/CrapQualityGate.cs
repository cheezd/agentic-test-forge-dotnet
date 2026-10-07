using AgenticTestForge.Cli;
using AgenticTestForge.Config;
using AgenticTestForge.Reporting;

namespace AgenticTestForge.Crap;

internal sealed class CrapQualityGate : IQualityGate
{
    public bool RunsFor(string verb) => verb is ForgeVerbs.Check or ForgeVerbs.Crap;

    public GateEvaluation Evaluate(RunPlan plan)
    {
        var result = CrapGate.Evaluate(plan);
        return new GateEvaluation(result.Gate, result.Error);
    }
}
