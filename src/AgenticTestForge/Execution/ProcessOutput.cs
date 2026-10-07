namespace AgenticTestForge.Execution;

internal sealed record ProcessOutput(int ExitCode, string Stdout, string Stderr);
