namespace AgenticTestForge.Reporting;

/// <summary>One survived or inconclusive mutant.</summary>
/// <param name="Filepath">Repo-relative path.</param>
/// <param name="QualifiedName">Member the mutant changed.</param>
/// <param name="Mutator">Mutator name.</param>
internal sealed record MutantRef(string Filepath, string QualifiedName, string Mutator);
