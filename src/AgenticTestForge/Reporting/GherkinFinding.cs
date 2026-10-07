namespace AgenticTestForge.Reporting;

internal sealed record GherkinFinding(
    string Scenario,
    string Filepath,
    double Score,
    int Killed,
    int Total,
    bool AboveThreshold
);
