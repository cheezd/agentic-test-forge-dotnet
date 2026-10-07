namespace AgenticTestForge.Gherkin;

internal sealed record ExampleRow(int LineIndex, IReadOnlyList<string> Cells);

internal sealed record ExampleTable(IReadOnlyList<string> Headers, IReadOnlyList<ExampleRow> Rows);

internal sealed record GherkinScenario(
    string Name,
    string Filepath,
    int StartLine,
    bool IsOutline,
    bool HasExamplesKeyword,
    ExampleTable? Examples,
    IReadOnlyList<string> Steps
);
