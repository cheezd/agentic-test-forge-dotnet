using AgenticTestForge.Cli;
using AgenticTestForge.Config;
using AgenticTestForge.Reporting;

namespace AgenticTestForge.Mutation;

internal sealed class MutationQualityGate : IQualityGate
{
    public bool RunsFor(string verb) => verb is ForgeVerbs.Check or ForgeVerbs.Mutate;

    public GateEvaluation Evaluate(RunPlan plan)
    {
        var result = MutationGate.Evaluate(plan);
        return new GateEvaluation(result.Gate, result.Error);
    }
}
