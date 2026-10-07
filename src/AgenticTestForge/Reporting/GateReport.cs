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
        new GherkinGateReport(status, threshold);

    public static IGateReport Dry(GateStatus status, IReadOnlyList<DryFinding> findings) =>
        new DryGateReport(status, findings);
}
