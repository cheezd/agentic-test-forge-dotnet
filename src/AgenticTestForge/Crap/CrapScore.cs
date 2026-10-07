namespace AgenticTestForge.Crap;

internal static class CrapScore
{
    public static double Compute(double complexity, double coverage)
    {
        var fraction = Math.Clamp(coverage, 0d, 1d);
        return (complexity * complexity * Math.Pow(1d - fraction, 3d)) + complexity;
    }
}
