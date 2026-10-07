using AgenticTestForge.Config;

namespace AgenticTestForge.Dry;

/// <summary>One duplication engine. Sonar and Jaccard are separate sources.</summary>
internal interface IDrySource
{
    string Id { get; }

    DryCollection Collect(RunPlan plan);
}
