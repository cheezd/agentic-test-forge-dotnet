namespace AgenticTestForge.Reporting;

internal static class ThresholdRules
{
    public static bool CrapIsAboveCeiling(double? crapScore, double ceiling) =>
        crapScore is null || crapScore.Value > ceiling;

    public static bool MutationIsUnderFloor(double score, double floor) => score < floor;
}
