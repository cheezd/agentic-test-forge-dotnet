using AgenticTestForge.Config;
using AgenticTestForge.Reporting;

namespace AgenticTestForge.Dry;

internal static class DryGate
{
    public static GateEvaluation Evaluate(RunPlan plan) =>
        Evaluate(plan, DrySourceCatalog.Select(plan.DrySources));

    internal static GateEvaluation Evaluate(RunPlan plan, IReadOnlyList<IDrySource> sources)
    {
        var ran = Ran(plan, sources);
        if (ran.Count == 0)
        {
            return Skipped();
        }

        return Advisory(ran);
    }

    private static List<DryCollection> Ran(RunPlan plan, IReadOnlyList<IDrySource> sources)
    {
        var ran = new List<DryCollection>();
        foreach (var source in sources)
        {
            var collection = source.Collect(plan);
            if (collection.Ran)
            {
                ran.Add(collection);
            }
        }

        return ran;
    }

    private static GateEvaluation Skipped() => new(GateReport.Dry(GateStatus.Skipped, []), null);

    private static GateEvaluation Advisory(IReadOnlyList<DryCollection> collections) =>
        new(
            GateReport.Dry(GateStatus.Advisory, Findings(collections), Condition(collections)),
            null
        );

    private static DryFinding[] Findings(IReadOnlyList<DryCollection> collections) =>
        collections.SelectMany(static collection => collection.Findings).ToArray();

    private static DuplicationsCondition? Condition(IReadOnlyList<DryCollection> collections)
    {
        foreach (var collection in collections)
        {
            if (collection.Condition is not null)
            {
                return collection.Condition;
            }
        }

        return null;
    }
}
