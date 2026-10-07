namespace AgenticTestForge.Mutation;

internal sealed record StrykerMutant(string Status, string Mutator, int Line);

internal sealed record StrykerFile(
    string Filepath,
    string Source,
    IReadOnlyList<StrykerMutant> Mutants
);
