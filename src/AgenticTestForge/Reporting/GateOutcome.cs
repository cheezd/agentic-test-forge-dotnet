namespace AgenticTestForge.Reporting;

/// <summary>One gate outcome used to decide the top-level status.</summary>
/// <param name="Name">Gate name, such as crap or mutation.</param>
/// <param name="Status">Outcome of that gate.</param>
internal sealed record GateOutcome(string Name, GateStatus Status);
