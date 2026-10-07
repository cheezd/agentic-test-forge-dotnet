namespace AgenticTestForge.Reporting;

internal static class GateReport
{
    public static IGateReport Crap(
        double threshold,
        GateStatus status,
        IReadOnlyList<CrapFinding> findings
    ) => new CrapGateReport(status, threshold, findings);

    public static IGateReport Mutation(
        double threshold,
        GateStatus status,
        IReadOnlyList<MutationFinding> findings,
        IReadOnlyList<MutantRef> survivors,
        IReadOnlyList<MutantRef> inconclusive
    ) => new MutationGateReport(status, threshold, findings, survivors, inconclusive);

    public static IGateReport Gherkin(double threshold, GateStatus status) =>
        new GherkinGateReport(status, threshold, [], [], [], false);

    public static IGateReport Gherkin(
        double threshold,
        GateStatus status,
        IReadOnlyList<GherkinFinding> findings,
        IReadOnlyList<MutantRef> survivors,
        IReadOnlyList<MutantRef> inconclusive
    ) => new GherkinGateReport(status, threshold, findings, survivors, inconclusive, true);

    public static IGateReport Dry(
        GateStatus status,
        IReadOnlyList<DryFinding> findings,
        DuplicationsCondition? condition = null
    ) => new DryGateReport(status, findings, condition);
}
