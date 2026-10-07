namespace AgenticTestForge.Gherkin;

internal sealed record ExampleMutation(
    int RowIndex,
    int ColumnIndex,
    string Original,
    string Mutated,
    ExampleRow Row
);
