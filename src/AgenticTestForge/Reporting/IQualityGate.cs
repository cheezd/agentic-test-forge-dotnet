using AgenticTestForge.Config;

namespace AgenticTestForge.Reporting;

internal interface IQualityGate
{
    bool RunsFor(string verb);

    GateEvaluation Evaluate(RunPlan plan);
}
