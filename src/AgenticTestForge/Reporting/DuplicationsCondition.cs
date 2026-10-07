namespace AgenticTestForge.Reporting;

/// <summary>Sonar quality-gate condition whose metric is a duplication measure.</summary>
/// <param name="Metric">Sonar metric key, such as <c>duplicated_lines_density</c>.</param>
/// <param name="Status">Condition status from the quality gate.</param>
/// <param name="Actual">Measured value, when Sonar sent one.</param>
internal sealed record DuplicationsCondition(string Metric, string Status, string? Actual);
