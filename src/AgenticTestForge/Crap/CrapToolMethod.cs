namespace AgenticTestForge.Crap;

internal sealed record CrapToolMethod(
    string Namespace,
    string ClassName,
    string MethodName,
    string FullName,
    string FilePath,
    double Complexity
);
