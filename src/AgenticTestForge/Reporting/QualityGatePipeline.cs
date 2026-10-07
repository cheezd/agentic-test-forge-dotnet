using AgenticTestForge.Config;
using AgenticTestForge.Crap;
using AgenticTestForge.Mutation;

namespace AgenticTestForge.Reporting;

internal static class QualityGatePipeline
{
    private static readonly IQualityGate[] DefaultGates =
    [
        new CrapQualityGate(),
        new MutationQualityGate(),
        new GherkinQualityGate(),
        new DryQualityGate(),
    ];

    public static RunResult Run(RunPlan plan) => Run(plan, DefaultGates);

    public static RunResult Run(RunPlan plan, IReadOnlyList<IQualityGate> gates)
    {
        var reports = new List<IGateReport>();
        var errors = new List<string>();
        foreach (var gate in gates)
        {
            if (!gate.RunsFor(plan.Verb))
            {
                continue;
            }

            var evaluation = gate.Evaluate(plan);
            reports.Add(evaluation.Report);
            if (evaluation.Error is not null)
            {
                errors.Add(evaluation.Error);
            }
        }

        return CheckReportBuilder.Finish(
            plan.Verb,
            plan.BaseRef,
            plan.Paths,
            reports,
            errors,
            toolError: errors.Count > 0
        );
    }
}
